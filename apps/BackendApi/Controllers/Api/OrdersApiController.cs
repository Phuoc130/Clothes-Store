using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductStore.Contracts.Shop;
using ProductStore.Models;
using ProductStore.Services;

namespace ProductStore.Controllers.Api
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersApiController : ControllerBase
    {
        private static readonly Dictionary<string, decimal> CouponPercentMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["WELCOME10"] = 10m,
            ["SALE15"] = 15m,
            ["VIP20"] = 20m
        };

        private readonly ProductDbContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly IOrderEmailService _orderEmailService;
        private readonly ILogger<OrdersApiController> _logger;

        public OrdersApiController(
            ProductDbContext context,
            IVnPayService vnPayService,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            IOrderEmailService orderEmailService,
            ILogger<OrdersApiController> logger)
        {
            _context = context;
            _vnPayService = vnPayService;
            _configuration = configuration;
            _environment = environment;
            _orderEmailService = orderEmailService;
            _logger = logger;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutOrderRequest? request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var fullName = request?.FullName?.Trim() ?? string.Empty;
            var phoneNumber = request?.PhoneNumber?.Trim() ?? string.Empty;
            var provinceCity = request?.ProvinceCity?.Trim() ?? string.Empty;
            var district = request?.District?.Trim() ?? string.Empty;
            var addressLine = request?.AddressLine?.Trim() ?? string.Empty;
            var orderNote = request?.OrderNote?.Trim();

            if (string.IsNullOrWhiteSpace(fullName)
                || string.IsNullOrWhiteSpace(phoneNumber)
                || string.IsNullOrWhiteSpace(provinceCity)
                || string.IsNullOrWhiteSpace(district)
                || string.IsNullOrWhiteSpace(addressLine))
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ thông tin giao hàng" });
            }

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (cartItems.Count == 0)
            {
                return BadRequest(new { success = false, message = "Giỏ hàng đang trống" });
            }

            foreach (var item in cartItems)
            {
                if (item.Product == null || !item.Product.Availability)
                {
                    return BadRequest(new { success = false, message = "Có sản phẩm không còn khả dụng trong giỏ hàng" });
                }

                if (item.Quantity > item.Product.Stock)
                {
                    return BadRequest(new { success = false, message = $"Sản phẩm {item.Product.Name} vượt quá tồn kho" });
                }

                if (!string.IsNullOrWhiteSpace(item.SelectedSize))
                {
                    var sizes = item.Product.GetSizes();
                    if (sizes.Count > 0 && !sizes.Contains(item.SelectedSize, StringComparer.OrdinalIgnoreCase))
                    {
                        return BadRequest(new { success = false, message = $"Size {item.SelectedSize} không hợp lệ cho sản phẩm {item.Product.Name}" });
                    }
                }
            }

            var subTotal = cartItems.Sum(i => i.Product!.FinalPrice * i.Quantity);
            var couponCode = request?.CouponCode?.Trim();
            var paymentMethod = request?.PaymentMethod?.Trim().ToLowerInvariant() ?? "cod";
            var discountPercent = GetCouponPercent(couponCode);
            var discountAmount = decimal.Round(subTotal * discountPercent / 100m, 0, MidpointRounding.AwayFromZero);
            var defaultShippingFee = _configuration.GetValue<decimal?>("Checkout:DefaultShippingFee") ?? 30000m;
            var shippingFee = request?.ShippingFee.HasValue == true
                ? Math.Max(0m, decimal.Round(request.ShippingFee.Value, 0, MidpointRounding.AwayFromZero))
                : Math.Max(0m, decimal.Round(defaultShippingFee, 0, MidpointRounding.AwayFromZero));
            var totalAmount = Math.Max(0m, subTotal - discountAmount + shippingFee);

            if (paymentMethod != "cod" && paymentMethod != "vnpay" && paymentMethod != "bankqr")
            {
                return BadRequest(new { success = false, message = "Phương thức thanh toán không hợp lệ" });
            }

            if (paymentMethod == "vnpay" && !_vnPayService.IsConfigured())
            {
                return BadRequest(new { success = false, message = "Chưa cấu hình VNPay. Vui lòng cấu hình TmnCode và HashSecret trong appsettings." });
            }

            var order = new ShopOrder
            {
                UserId = userId,
                SubTotalAmount = subTotal,
                DiscountAmount = discountAmount,
                ShippingFee = shippingFee,
                TotalAmount = totalAmount,
                FullName = fullName,
                PhoneNumber = phoneNumber,
                ProvinceCity = provinceCity,
                District = district,
                AddressLine = addressLine,
                OrderNote = string.IsNullOrWhiteSpace(orderNote) ? null : orderNote,
                PaymentMethod = paymentMethod,
                CouponCode = string.IsNullOrWhiteSpace(couponCode) ? null : couponCode,
                Status = paymentMethod == "vnpay"
                    ? "PendingPayment"
                    : paymentMethod == "bankqr"
                        ? "PendingTransfer"
                        : "Pending",
                CreatedAt = DateTime.UtcNow,
                Items = cartItems.Select(i => new ShopOrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product!.FinalPrice
                }).ToList()
            };

            foreach (var item in cartItems)
            {
                item.Product!.Stock -= item.Quantity;
                if (item.Product.Stock <= 0)
                {
                    item.Product.Stock = 0;
                    item.Product.Availability = false;
                }
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            _context.ShopOrders.Add(order);
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            await TrySendOrderEmailAsync(userId, fullName, order, cartItems);

            if (paymentMethod == "bankqr")
            {
                var bankId = _configuration["BankQr:BankId"] ?? "970405";
                var bankName = _configuration["BankQr:BankName"] ?? "Agribank";
                var accountNo = _configuration["BankQr:AccountNo"] ?? "690820518070";
                var accountName = _configuration["BankQr:AccountName"] ?? "NGUYEN TAN KHOA";
                var transferNote = $"DH{order.ShopOrderId}";
                var qrUrl = BuildVietQrUrl(bankId, accountNo, accountName, transferNote, totalAmount);
                var clientRedirectUrl = BuildBankTransferClientUrl(order.ShopOrderId, totalAmount, bankName, accountNo, accountName, transferNote, qrUrl);

                return Ok(new
                {
                    success = true,
                    message = "Tạo thanh toán chuyển khoản thành công",
                    orderId = order.ShopOrderId,
                    status = order.Status,
                    subTotal,
                    discountPercent,
                    discountAmount,
                    shippingFee,
                    totalAmount,
                    paymentMethod,
                    paymentRequired = true,
                    bankName,
                    accountNo,
                    accountName,
                    transferNote,
                    qrUrl,
                    clientRedirectUrl,
                    shippingAddress = new
                    {
                        fullName,
                        phoneNumber,
                        provinceCity,
                        district,
                        addressLine
                    },
                    orderNote = string.IsNullOrWhiteSpace(orderNote) ? null : orderNote,
                    couponCode = string.IsNullOrWhiteSpace(couponCode) ? null : couponCode
                });
            }

            if (paymentMethod == "vnpay")
            {
                var paymentUrl = _vnPayService.BuildPaymentUrl(
                    order.ShopOrderId,
                    totalAmount,
                    $"Thanh toan don hang #{order.ShopOrderId}",
                    GetClientIpAddress());

                return Ok(new
                {
                    success = true,
                    message = "Tạo thanh toán VNPay thành công",
                    orderId = order.ShopOrderId,
                    status = order.Status,
                    subTotal,
                    discountPercent,
                    discountAmount,
                    shippingFee,
                    totalAmount,
                    paymentMethod,
                    paymentRequired = true,
                    paymentUrl,
                    shippingAddress = new
                    {
                        fullName,
                        phoneNumber,
                        provinceCity,
                        district,
                        addressLine
                    },
                    orderNote = string.IsNullOrWhiteSpace(orderNote) ? null : orderNote,
                    couponCode = string.IsNullOrWhiteSpace(couponCode) ? null : couponCode
                });
            }

            return Ok(new
            {
                success = true,
                message = "Đặt hàng thành công",
                orderId = order.ShopOrderId,
                status = order.Status,
                subTotal,
                discountPercent,
                discountAmount,
                shippingFee,
                totalAmount,
                paymentMethod,
                paymentRequired = false,
                shippingAddress = new
                {
                    fullName,
                    phoneNumber,
                    provinceCity,
                    district,
                    addressLine
                },
                orderNote = string.IsNullOrWhiteSpace(orderNote) ? null : orderNote,
                couponCode = string.IsNullOrWhiteSpace(couponCode) ? null : couponCode
            });
        }

        [HttpGet("vnpay-return")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayReturn()
        {
            var queryParams = Request.Query
                .ToDictionary(k => k.Key, v => v.Value.ToString(), StringComparer.OrdinalIgnoreCase);

            var clientReturnUrl = _configuration["VnPay:ClientReturnUrl"] ?? "http://localhost:5206/Product/PaymentResult";

            if (!_vnPayService.TryValidateReturn(queryParams, out var orderId, out var isSuccess, out var message))
            {
                return Redirect(BuildClientReturnUrl(clientReturnUrl, orderId, "PaymentFailed", message));
            }

            var order = await _context.ShopOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.ShopOrderId == orderId);

            if (order == null)
            {
                return Redirect(BuildClientReturnUrl(clientReturnUrl, orderId, "PaymentFailed", "Không tìm thấy đơn hàng"));
            }

            if (isSuccess)
            {
                if (string.Equals(order.Status, "PendingPayment", StringComparison.OrdinalIgnoreCase))
                {
                    order.Status = "Paid";
                    await _context.SaveChangesAsync();
                }

                return Redirect(BuildClientReturnUrl(clientReturnUrl, orderId, "Paid", "Thanh toán thành công"));
            }

            if (string.Equals(order.Status, "PendingPayment", StringComparison.OrdinalIgnoreCase))
            {
                await RestoreStockForOrder(order);
                order.Status = "PaymentFailed";
                await _context.SaveChangesAsync();
            }

            return Redirect(BuildClientReturnUrl(clientReturnUrl, orderId, "PaymentFailed", message));
        }

        [HttpPost("coupon/validate")]
        public IActionResult ValidateCoupon([FromBody] CheckoutOrderRequest? request)
        {
            var code = request?.CouponCode?.Trim();
            var percent = GetCouponPercent(code);
            if (percent <= 0)
            {
                return Ok(new { valid = false, discountPercent = 0, message = "Mã giảm giá không hợp lệ" });
            }

            return Ok(new { valid = true, discountPercent = percent, message = "Áp dụng mã thành công" });
        }

        [HttpGet("my")]
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var orderEntities = await _context.ShopOrders
                .AsNoTracking()
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var orders = orderEntities.Select(o => new
            {
                o.ShopOrderId,
                o.SubTotalAmount,
                o.DiscountAmount,
                o.ShippingFee,
                o.TotalAmount,
                o.Status,
                o.CreatedAt,
                o.FullName,
                o.PhoneNumber,
                o.ProvinceCity,
                o.District,
                o.AddressLine,
                o.OrderNote,
                o.PaymentMethod,
                o.CouponCode,
                hasTransferProof = HasTransferProof(o.ShopOrderId),
                transferProofUrl = GetTransferProofUrl(o.ShopOrderId),
                itemCount = o.Items.Sum(i => i.Quantity),
                items = o.Items.Select(i => new
                {
                    i.ProductId,
                    productName = i.Product != null ? i.Product.Name : "Unknown",
                    i.Quantity,
                    i.UnitPrice,
                    imageUrl = i.Product != null ? i.Product.ImageUrl : null
                })
            });

            return Ok(orders);
        }

        [HttpPost("{id:int}/transfer-proof")]
        public async Task<IActionResult> UploadTransferProof(int id, IFormFile? proofImage)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var order = await _context.ShopOrders.FirstOrDefaultAsync(o => o.ShopOrderId == id && o.UserId == userId);
            if (order == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            if (!string.Equals(order.Status, "PendingTransfer", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(order.Status, "TransferSubmitted", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Đơn hàng không ở trạng thái chờ chuyển khoản" });
            }

            if (proofImage != null && proofImage.Length > 0)
            {
                var ext = Path.GetExtension(proofImage.FileName)?.ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (string.IsNullOrWhiteSpace(ext) || !allowed.Contains(ext))
                {
                    return BadRequest(new { success = false, message = "Định dạng ảnh không hỗ trợ" });
                }

                var folder = EnsureProofFolder();
                DeleteOldProofFiles(id);

                var fileName = $"order-{id}-{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
                var fullPath = Path.Combine(folder, fileName);

                await using (var fs = new FileStream(fullPath, FileMode.CreateNew))
                {
                    await proofImage.CopyToAsync(fs);
                }
            }

            order.Status = "TransferSubmitted";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = proofImage != null && proofImage.Length > 0
                    ? "Đã tải biên lai thành công"
                    : "Đã ghi nhận xác nhận chuyển khoản",
                orderId = id,
                status = order.Status,
                transferProofUrl = $"/api/orders/proof/{id}"
            });
        }

        [HttpGet("proof/{id:int}")]
        [AllowAnonymous]
        public IActionResult GetTransferProof(int id)
        {
            var file = GetProofFilePath(id);
            if (string.IsNullOrWhiteSpace(file) || !System.IO.File.Exists(file))
            {
                return NotFound();
            }

            var ext = Path.GetExtension(file).ToLowerInvariant();
            var contentType = ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return PhysicalFile(file, contentType);
        }

        [HttpPost("admin/{id:int}/confirm-transfer")]
        [AllowAnonymous]
        public async Task<IActionResult> AdminConfirmTransfer(int id)
        {
            var order = await _context.ShopOrders.FirstOrDefaultAsync(o => o.ShopOrderId == id);
            if (order == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            var canConfirm = string.Equals(order.Status, "TransferSubmitted", StringComparison.OrdinalIgnoreCase)
                || string.Equals(order.Status, "PendingTransfer", StringComparison.OrdinalIgnoreCase);
            if (!canConfirm)
            {
                return BadRequest(new { success = false, message = "Đơn hàng không hợp lệ để xác nhận thanh toán" });
            }

            order.Status = "Paid";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xác nhận thanh toán thành công", orderId = id, status = order.Status });
        }

        [HttpPost("bank-transfer/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> BankTransferWebhook([FromBody] JsonElement payload)
        {
            var configuredKey = _configuration["BankWebhook:SecretKey"];
            if (!string.IsNullOrWhiteSpace(configuredKey))
            {
                var providedKey = Request.Headers["X-Bank-Webhook-Key"].FirstOrDefault()
                    ?? Request.Headers["x-bank-webhook-key"].FirstOrDefault();
                if (!string.Equals(configuredKey, providedKey, StringComparison.Ordinal))
                {
                    return Unauthorized(new { success = false, message = "Invalid webhook key" });
                }
            }

            var transferContent = GetPayloadString(payload, "transferContent")
                ?? GetPayloadString(payload, "description")
                ?? GetPayloadString(payload, "content")
                ?? string.Empty;

            var amount = GetPayloadDecimal(payload, "transferAmount")
                ?? GetPayloadDecimal(payload, "amount")
                ?? 0m;

            var orderId = TryExtractOrderId(transferContent);
            if (orderId <= 0)
            {
                return Ok(new { success = false, message = "No DH{orderId} found in transfer content" });
            }

            var order = await _context.ShopOrders
                .FirstOrDefaultAsync(o => o.ShopOrderId == orderId);
            if (order == null)
            {
                return Ok(new { success = false, message = "Order not found", orderId });
            }

            if (string.Equals(order.Status, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { success = true, message = "Order already paid", orderId, status = order.Status });
            }

            var expected = decimal.Round(order.TotalAmount, 0, MidpointRounding.AwayFromZero);
            var incoming = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
            if (incoming < expected)
            {
                return Ok(new
                {
                    success = false,
                    message = "Amount is lower than order total",
                    orderId,
                    expectedAmount = expected,
                    receivedAmount = incoming
                });
            }

            var validStatuses = new[] { "PendingTransfer", "TransferSubmitted", "PendingPayment", "PaymentFailed" };
            if (!validStatuses.Contains(order.Status, StringComparer.OrdinalIgnoreCase))
            {
                return Ok(new { success = false, message = "Order status is not payable", orderId, status = order.Status });
            }

            order.Status = "Paid";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Auto reconciled transfer successfully",
                orderId,
                status = order.Status,
                expectedAmount = expected,
                receivedAmount = incoming
            });
        }

        [HttpPost("{id:int}/repay")]
        public async Task<IActionResult> RepayOrder(int id, [FromBody] CheckoutOrderRequest? request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var order = await _context.ShopOrders.FirstOrDefaultAsync(o => o.ShopOrderId == id && o.UserId == userId);
            if (order == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            var paymentMethod = request?.PaymentMethod?.Trim().ToLowerInvariant() ?? "vnpay";
            if (paymentMethod != "vnpay" && paymentMethod != "bankqr")
            {
                return BadRequest(new { success = false, message = "Phương thức thanh toán lại không hợp lệ" });
            }

            var canRepay = string.Equals(order.Status, "PendingPayment", StringComparison.OrdinalIgnoreCase)
                || string.Equals(order.Status, "PaymentFailed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(order.Status, "PendingTransfer", StringComparison.OrdinalIgnoreCase)
                || string.Equals(order.Status, "TransferSubmitted", StringComparison.OrdinalIgnoreCase);

            if (!canRepay)
            {
                return BadRequest(new { success = false, message = "Đơn hàng này không thể thanh toán lại" });
            }

            if (paymentMethod == "vnpay")
            {
                if (!_vnPayService.IsConfigured())
                {
                    return BadRequest(new { success = false, message = "Chưa cấu hình VNPay. Vui lòng cấu hình TmnCode và HashSecret trong appsettings." });
                }

                order.Status = "PendingPayment";
                await _context.SaveChangesAsync();

                var paymentUrl = _vnPayService.BuildPaymentUrl(
                    order.ShopOrderId,
                    order.TotalAmount,
                    $"Thanh toan lai don hang #{order.ShopOrderId}",
                    GetClientIpAddress());

                return Ok(new
                {
                    success = true,
                    orderId = order.ShopOrderId,
                    status = order.Status,
                    paymentMethod,
                    paymentRequired = true,
                    paymentUrl
                });
            }

            var bankId = _configuration["BankQr:BankId"] ?? "970405";
            var bankName = _configuration["BankQr:BankName"] ?? "Agribank";
            var accountNo = _configuration["BankQr:AccountNo"] ?? "690820518070";
            var accountName = _configuration["BankQr:AccountName"] ?? "NGUYEN TAN KHOA";
            var transferNote = $"DH{order.ShopOrderId}";
            var qrUrl = BuildVietQrUrl(bankId, accountNo, accountName, transferNote, order.TotalAmount);
            var clientRedirectUrl = BuildBankTransferClientUrl(order.ShopOrderId, order.TotalAmount, bankName, accountNo, accountName, transferNote, qrUrl);

            order.Status = "PendingTransfer";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                orderId = order.ShopOrderId,
                status = order.Status,
                paymentMethod,
                paymentRequired = true,
                bankName,
                accountNo,
                accountName,
                transferNote,
                qrUrl,
                clientRedirectUrl
            });
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var order = await _context.ShopOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.ShopOrderId == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            var cancellableStatuses = new[] { "Pending", "PendingPayment", "PendingTransfer" };
            if (!cancellableStatuses.Contains(order.Status, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Chỉ có thể hủy đơn ở trạng thái chờ xử lý hoặc chờ thanh toán" });
            }

            await RestoreStockForOrder(order);

            order.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã hủy đơn hàng" });
        }

        private static decimal GetCouponPercent(string? couponCode)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return 0m;
            }

            return CouponPercentMap.TryGetValue(couponCode, out var value) ? value : 0m;
        }

        private async Task RestoreStockForOrder(ShopOrder order)
        {
            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products.Where(p => productIds.Contains(p.Product_ID)).ToListAsync();
            var productMap = products.ToDictionary(p => p.Product_ID);

            foreach (var item in order.Items)
            {
                if (!productMap.TryGetValue(item.ProductId, out var product))
                {
                    continue;
                }

                product.Stock += item.Quantity;
                if (product.Stock > 0)
                {
                    product.Availability = true;
                }
            }
        }

        private async Task TrySendOrderEmailAsync(string userId, string fullName, ShopOrder order, List<CartItem> cartItems)
        {
            try
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                var toEmail = user?.Email;
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    return;
                }

                var items = cartItems
                    .Where(i => i.Product != null)
                    .Select(i => (i.Product!.Name, i.Quantity, i.Product.FinalPrice))
                    .ToList();

                await _orderEmailService.SendOrderPlacedEmailAsync(
                    toEmail,
                    string.IsNullOrWhiteSpace(fullName) ? "Ban" : fullName,
                    order.ShopOrderId,
                    order.TotalAmount,
                    items);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Order {OrderId} was created but email notification failed", order.ShopOrderId);
            }
        }

        private string GetClientIpAddress()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrWhiteSpace(ip))
            {
                return "127.0.0.1";
            }

            if (ip == "::1")
            {
                return "127.0.0.1";
            }

            return ip;
        }

        private static string BuildClientReturnUrl(string baseUrl, int orderId, string status, string message)
        {
            var sep = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            return $"{baseUrl}{sep}orderId={orderId}&status={Uri.EscapeDataString(status)}&message={Uri.EscapeDataString(message)}";
        }

        private string EnsureProofFolder()
        {
            var folder = Path.Combine(_environment.ContentRootPath, "wwwroot", "payment-proofs");
            Directory.CreateDirectory(folder);
            return folder;
        }

        private void DeleteOldProofFiles(int orderId)
        {
            var folder = EnsureProofFolder();
            var pattern = $"order-{orderId}-*.*";
            foreach (var file in Directory.GetFiles(folder, pattern))
            {
                try
                {
                    System.IO.File.Delete(file);
                }
                catch
                {
                    // Ignore delete failures to avoid blocking new uploads.
                }
            }
        }

        private bool HasTransferProof(int orderId)
        {
            var folder = EnsureProofFolder();
            var pattern = $"order-{orderId}-*.*";
            return Directory.GetFiles(folder, pattern).Length > 0;
        }

        private string? GetTransferProofUrl(int orderId)
        {
            if (string.IsNullOrWhiteSpace(GetProofFilePath(orderId)))
            {
                return null;
            }

            return $"/api/orders/proof/{orderId}";
        }

        private string? GetProofFilePath(int orderId)
        {
            var folder = EnsureProofFolder();
            var pattern = $"order-{orderId}-*.*";
            return Directory.GetFiles(folder, pattern)
                .OrderByDescending(f => f)
                .FirstOrDefault();
        }

        private static string BuildVietQrUrl(string bankId, string accountNo, string accountName, string transferNote, decimal amount)
        {
            var roundedAmount = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
            var amountText = ((long)roundedAmount).ToString();
            var escapedName = Uri.EscapeDataString(accountName);
            var escapedNote = Uri.EscapeDataString(transferNote);
            return $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={amountText}&addInfo={escapedNote}&accountName={escapedName}";
        }

        private string BuildBankTransferClientUrl(int orderId, decimal totalAmount, string bankName, string accountNo, string accountName, string transferNote, string qrUrl)
        {
            var pageUrl = _configuration["BankQr:ClientPaymentPage"] ?? "http://localhost:5206/Product/BankTransferPayment";
            var sep = pageUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            var amountText = decimal.Round(totalAmount, 0, MidpointRounding.AwayFromZero).ToString();

            return $"{pageUrl}{sep}orderId={orderId}&amount={Uri.EscapeDataString(amountText)}&bankName={Uri.EscapeDataString(bankName)}&accountNo={Uri.EscapeDataString(accountNo)}&accountName={Uri.EscapeDataString(accountName)}&transferNote={Uri.EscapeDataString(transferNote)}&qrUrl={Uri.EscapeDataString(qrUrl)}";
        }

        private static string? GetPayloadString(JsonElement payload, string property)
        {
            if (!payload.TryGetProperty(property, out var value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                _ => null
            };
        }

        private static decimal? GetPayloadDecimal(JsonElement payload, string property)
        {
            if (!payload.TryGetProperty(property, out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var n))
            {
                return n;
            }

            if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var s))
            {
                return s;
            }

            return null;
        }

        private static int TryExtractOrderId(string transferContent)
        {
            if (string.IsNullOrWhiteSpace(transferContent))
            {
                return 0;
            }

            var markerIndex = transferContent.IndexOf("DH", StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                return 0;
            }

            markerIndex += 2;
            var digits = new List<char>();
            while (markerIndex < transferContent.Length && char.IsDigit(transferContent[markerIndex]))
            {
                digits.Add(transferContent[markerIndex]);
                markerIndex++;
            }

            if (digits.Count == 0)
            {
                return 0;
            }

            return int.TryParse(new string(digits.ToArray()), out var orderId) ? orderId : 0;
        }
    }
}

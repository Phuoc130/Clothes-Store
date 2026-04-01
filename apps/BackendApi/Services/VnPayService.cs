using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ProductStore.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool IsConfigured()
        {
            var tmnCode = _configuration["VnPay:TmnCode"];
            var hashSecret = _configuration["VnPay:HashSecret"];
            var baseUrl = _configuration["VnPay:BaseUrl"];
            var returnUrl = _configuration["VnPay:ReturnUrl"];

            return !string.IsNullOrWhiteSpace(tmnCode)
                && !string.IsNullOrWhiteSpace(hashSecret)
                && !string.IsNullOrWhiteSpace(baseUrl)
                && !string.IsNullOrWhiteSpace(returnUrl);
        }

        public string BuildPaymentUrl(int orderId, decimal amount, string description, string clientIp)
        {
            var tmnCode = _configuration["VnPay:TmnCode"] ?? string.Empty;
            var hashSecret = _configuration["VnPay:HashSecret"] ?? string.Empty;
            var baseUrl = _configuration["VnPay:BaseUrl"] ?? string.Empty;
            var returnUrl = _configuration["VnPay:ReturnUrl"] ?? string.Empty;

            var now = DateTime.UtcNow.AddHours(7);
            var txnRef = $"{orderId}_{now:yyyyMMddHHmmss}";
            var amountVnd = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);

            var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Amount"] = ((long)(amountVnd * 100)).ToString(CultureInfo.InvariantCulture),
                ["vnp_Command"] = "pay",
                ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp,
                ["vnp_Locale"] = "vn",
                ["vnp_OrderInfo"] = description,
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TmnCode"] = tmnCode,
                ["vnp_TxnRef"] = txnRef,
                ["vnp_Version"] = "2.1.0"
            };

            var queryWithoutHash = BuildQueryString(data);
            var secureHash = HmacSha512(hashSecret, queryWithoutHash);

            return $"{baseUrl}?{queryWithoutHash}&vnp_SecureHash={secureHash}";
        }

        public bool TryValidateReturn(IDictionary<string, string> queryParams, out int orderId, out bool isSuccess, out string message)
        {
            orderId = 0;
            isSuccess = false;
            message = "Thanh toán thất bại";

            var hashSecret = _configuration["VnPay:HashSecret"] ?? string.Empty;
            var receivedSecureHash = queryParams.TryGetValue("vnp_SecureHash", out var h) ? h : string.Empty;
            var txnRef = queryParams.TryGetValue("vnp_TxnRef", out var t) ? t : string.Empty;
            var responseCode = queryParams.TryGetValue("vnp_ResponseCode", out var c) ? c : string.Empty;

            if (string.IsNullOrWhiteSpace(receivedSecureHash) || string.IsNullOrWhiteSpace(txnRef))
            {
                message = "Thiếu dữ liệu xác thực thanh toán";
                return false;
            }

            var signData = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var kv in queryParams)
            {
                if (string.Equals(kv.Key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(kv.Key, "vnp_SecureHashType", StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(kv.Value))
                {
                    continue;
                }

                signData[kv.Key] = kv.Value;
            }

            var raw = BuildQueryString(signData);
            var expectedHash = HmacSha512(hashSecret, raw);

            if (!string.Equals(expectedHash, receivedSecureHash, StringComparison.OrdinalIgnoreCase))
            {
                message = "Chữ ký thanh toán không hợp lệ";
                return false;
            }

            var refParts = txnRef.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (refParts.Length == 0 || !int.TryParse(refParts[0], out orderId))
            {
                message = "Không xác định được đơn hàng thanh toán";
                return false;
            }

            isSuccess = string.Equals(responseCode, "00", StringComparison.OrdinalIgnoreCase);
            message = isSuccess ? "Thanh toán thành công" : "Thanh toán thất bại hoặc đã hủy";
            return true;
        }

        private static string BuildQueryString(SortedDictionary<string, string> data)
        {
            return string.Join("&", data.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        }

        private static string HmacSha512(string key, string input)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(input);
            using var hmac = new HMACSHA512(keyBytes);
            var hash = hmac.ComputeHash(inputBytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}

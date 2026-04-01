using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ProductStore.Services
{
    public class OrderEmailService : IOrderEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderEmailService> _logger;

        public OrderEmailService(IConfiguration configuration, ILogger<OrderEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendOrderPlacedEmailAsync(
            string toEmail,
            string customerName,
            int orderId,
            decimal totalAmount,
            IEnumerable<(string productName, int quantity, decimal unitPrice)> items)
        {
            var smtpEnabled = _configuration.GetValue<bool?>("Smtp:Enabled") ?? false;
            if (!smtpEnabled)
            {
                return;
            }

            var host = _configuration["Smtp:Host"] ?? string.Empty;
            var port = _configuration.GetValue<int?>("Smtp:Port") ?? 587;
            var userName = _configuration["Smtp:User"] ?? string.Empty;
            var password = _configuration["Smtp:Pass"] ?? string.Empty;
            var fromEmail = _configuration["Smtp:FromEmail"] ?? userName;
            var fromName = _configuration["Smtp:FromName"] ?? "Clothes Store";
            var orderTrackingBaseUrl = _configuration["Smtp:OrderTrackingUrl"] ?? "http://localhost:5206/Product/MyOrders";

            if (string.IsNullOrWhiteSpace(host)
                || string.IsNullOrWhiteSpace(fromEmail)
                || string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("SMTP config is incomplete. Skip sending order email for order {OrderId}", orderId);
                return;
            }

            var itemRows = string.Join("", items.Select(item =>
                $"<tr><td style='padding:6px 0'>{item.productName}</td><td style='padding:6px 0;text-align:center'>{item.quantity}</td><td style='padding:6px 0;text-align:right'>{item.unitPrice:N0} VND</td></tr>"));

            var bodyHtml = $@"
<div style='font-family:Segoe UI,Arial,sans-serif;color:#1f2937;line-height:1.6'>
    <h2 style='margin:0 0 12px'>Cam on ban da dat hang!</h2>
    <p>Xin chao <strong>{customerName}</strong>, don hang <strong>#{orderId}</strong> da duoc tao thanh cong.</p>
    <table style='width:100%;border-collapse:collapse;margin:14px 0'>
        <thead>
            <tr>
                <th style='text-align:left;border-bottom:1px solid #e5e7eb;padding:8px 0'>San pham</th>
                <th style='text-align:center;border-bottom:1px solid #e5e7eb;padding:8px 0'>SL</th>
                <th style='text-align:right;border-bottom:1px solid #e5e7eb;padding:8px 0'>Don gia</th>
            </tr>
        </thead>
        <tbody>{itemRows}</tbody>
    </table>
    <p><strong>Tong thanh toan:</strong> {totalAmount:N0} VND</p>
    <p>Theo doi trang thai don hang tai day: <a href='{orderTrackingBaseUrl}'>My Orders</a></p>
</div>";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = $"[Clothes Store] Xac nhan don hang #{orderId}";
            message.Body = new BodyBuilder { HtmlBody = bodyHtml }.ToMessageBody();

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTlsWhenAvailable);
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    await client.AuthenticateAsync(userName, password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order email for order {OrderId}", orderId);
            }
        }
    }
}

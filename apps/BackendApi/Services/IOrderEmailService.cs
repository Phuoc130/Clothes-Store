namespace ProductStore.Services
{
    public interface IOrderEmailService
    {
        Task SendOrderPlacedEmailAsync(
            string toEmail,
            string customerName,
            int orderId,
            decimal totalAmount,
            IEnumerable<(string productName, int quantity, decimal unitPrice)> items);
    }
}

namespace ProductStore.Services
{
    public interface IVnPayService
    {
        bool IsConfigured();

        string BuildPaymentUrl(int orderId, decimal amount, string description, string clientIp);

        bool TryValidateReturn(IDictionary<string, string> queryParams, out int orderId, out bool isSuccess, out string message);
    }
}

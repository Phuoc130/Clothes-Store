namespace ProductStore.Contracts.Shop
{
    public class CheckoutOrderRequest
    {
        public string? CouponCode { get; set; }

        public string PaymentMethod { get; set; } = "cod";

        public string? FullName { get; set; }

        public string? PhoneNumber { get; set; }

        public string? ProvinceCity { get; set; }

        public string? District { get; set; }

        public string? AddressLine { get; set; }

        public string? OrderNote { get; set; }

        public decimal? ShippingFee { get; set; }
    }
}

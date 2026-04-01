namespace ProductStore.Contracts.Shop
{
    public class AddCartItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}


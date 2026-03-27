using System.Text.Json.Serialization;

namespace ProductStore.Contracts.Shop
{
    public class AddCartItemRequest
    {
        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; } = 1;

        [JsonPropertyName("size")]
        public string? Size { get; set; }
    }
}

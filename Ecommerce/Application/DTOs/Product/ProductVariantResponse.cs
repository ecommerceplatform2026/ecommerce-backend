namespace Application.DTOs.Product
{
    public sealed class ProductVariantResponse
    {
        public Guid Id { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Size { get; set; }
        public long Stock { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public long Price { get; set; }
    }
}

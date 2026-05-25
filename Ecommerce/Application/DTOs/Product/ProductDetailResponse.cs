using Domain.Enums;

namespace Application.DTOs.Product
{
    public sealed class ProductDetailResponse
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Material { get; set; }
        public long BasePrice { get; set; }
        public long Price { get; set; }
        public long MinPrice { get; set; }
        public long MaxPrice { get; set; }
        public ProductStatus Status { get; set; }
        public string? CategoryName { get; set; }
        public int TotalStock { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public List<ProductImageResponse> Images { get; set; } = new();
        public List<ProductVariantResponse> Variants { get; set; } = new();
    }

    public sealed class ProductImageResponse
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public sealed class ProductVariantResponse
    {
        public Guid Id { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Size { get; set; }
        public int Stock { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public long Price { get; set; }
    }
}

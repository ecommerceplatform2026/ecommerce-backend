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
        public long TotalStock { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public List<ProductImageResponse> Images { get; set; } = new();
        public List<ProductVariantResponse> Variants { get; set; } = new();
    }
}

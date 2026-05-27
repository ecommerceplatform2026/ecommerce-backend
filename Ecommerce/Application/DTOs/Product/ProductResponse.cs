using Domain.Enums;

namespace Application.DTOs.Product
{
    public sealed record ProductResponse(
        Guid Id,
        Guid CategoryId,
        string Name,
        string? Description,
        string? Material,
        long BasePrice,
        ProductStatus Status,
        string? CategoryName,
        string? ImageUrl,
        long MinPrice,
        long MaxPrice,
        long TotalStock,
        string StockStatus,
        double AverageRating,
        int ReviewCount,
        List<Application.DTOs.Product.ProductVariants.ProductVariantResponse> Variants);
}

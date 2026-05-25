using Application.DTOs.Product.ProductVariant;
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
        List<ProductVariantResponse> Variants);
}

namespace Application.DTOs.Product.ProductVariant
{
    public sealed record ProductVariantResponse(
        Guid Id,
        Guid ProductId,
        string SKU,
        string? Color,
        string? Size,
        long Stock,
        long LowStockThreshold,
        long Price,
        bool IsLowStock,
        bool IsOutOfStock);
}

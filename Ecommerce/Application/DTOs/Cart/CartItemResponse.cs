namespace Application.DTOs.Cart
{
    public sealed record CartItemResponse(
        Guid Id,
        Guid ProductVariantId,
        Guid ProductId,
        string ProductName,
        string? ProductImageUrl,
        string SKU,
        string? Color,
        string? Size,
        long Price,
        int Quantity,
        long Stock,
        bool IsLowStock,
        bool IsOutOfStock);
}

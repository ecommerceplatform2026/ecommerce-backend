using System;

namespace Application.DTOs.Wishlist
{
    public sealed record WishlistItemResponse(
        Guid Id,
        Guid ProductVariantId,
        Guid ProductId,
        string ProductName,
        string? ProductImageUrl,
        string SKU,
        string? Color,
        string? Size,
        long Price,
        long Stock,
        bool IsOutOfStock,
        bool IsLowStock,
        string StockStatus);
}

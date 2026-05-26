using Application.DTOs.Product.ProductVariants;
using Domain.Entities;

namespace Application.Mappings
{
    public static class ProductVariantMappingExtensions
    {
        public static ProductVariantResponse ToProductVariantResponse(this ProductVariant variant)
        {
            return new ProductVariantResponse(
                variant.Id,
                variant.ProductId,
                variant.SKU,
                variant.Color,
                variant.Size,
                variant.Stock,
                variant.LowStockThreshold,
                variant.Price,
                variant.IsLowStock(),
                variant.IsOutOfStock());
        }
    }
}

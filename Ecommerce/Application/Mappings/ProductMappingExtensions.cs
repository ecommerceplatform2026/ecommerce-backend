using Application.DTOs.Product;
using Application.DTOs.Product.ProductVariant;
using Domain.Entities;

namespace Application.Mappings
{
    public static class ProductMappingExtensions
    {
        public static ProductResponse ToProductResponse(this Product product)
        {
            var variants = product.ProductVariants?
                .Where(v => !v.IsDeleted)
                .Select(v => v.ToProductVariantResponse())
                .ToList() ?? new List<ProductVariantResponse>();

            return new ProductResponse(
                product.Id,
                product.CategoryId,
                product.Name,
                product.Description,
                product.Material,
                product.BasePrice,
                product.Status,
                product.Category?.Name,
                variants);
        }

        public static Product ToEntity(this CreateProductRequest request)
        {
            return Product.Create(
                request.CategoryId,
                request.Name,
                request.Description,
                request.Material,
                request.BasePrice,
                request.Status);
        }

        public static void MapToEntity(this UpdateProductRequest request, Product product)
        {
            product.Update(
                request.CategoryId,
                request.Name,
                request.Description,
                request.Material,
                request.BasePrice,
                request.Status);
        }
    }
}

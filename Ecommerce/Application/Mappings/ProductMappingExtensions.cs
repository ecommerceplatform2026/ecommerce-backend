using Application.DTOs.Product;
using Domain.Entities;

namespace Application.Mappings
{
    public static class ProductMappingExtensions
    {
        public static ProductResponse ToProductResponse(this Product product)
        {
            return new ProductResponse
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                Material = product.Material,
                BasePrice = product.BasePrice,
                Status = product.Status,
                CategoryName = product.Category?.Name
            };
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

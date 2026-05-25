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

        public static ProductDetailResponse ToProductDetailResponse(this Product product)
        {
            var variants = product.ProductVariants
                .OrderBy(variant => variant.SKU)
                .Select(variant => new ProductVariantResponse
                {
                    Id = variant.Id,
                    SKU = variant.SKU,
                    Color = variant.Color,
                    Size = variant.Size,
                    Stock = variant.Stock,
                    StockStatus = GetStockStatus(variant.Stock),
                    Price = variant.Price
                })
                .ToList();

            var prices = variants.Select(variant => variant.Price).DefaultIfEmpty(product.BasePrice).ToList();
            var totalStock = variants.Sum(variant => variant.Stock);

            return new ProductDetailResponse
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                Material = product.Material,
                BasePrice = product.BasePrice,
                Price = prices.Min(),
                MinPrice = prices.Min(),
                MaxPrice = prices.Max(),
                Status = product.Status,
                CategoryName = product.Category?.Name,
                TotalStock = totalStock,
                StockStatus = GetStockStatus(totalStock),
                Images = product.ProductImages
                    .OrderBy(image => image.CreatedAt)
                    .Select(image => new ProductImageResponse
                    {
                        Id = image.Id,
                        ImageUrl = image.ImageUrl
                    })
                    .ToList(),
                Variants = variants
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

        private static string GetStockStatus(int stock)
        {
            return stock > 0 ? "InStock" : "OutOfStock";
        }
    }
}

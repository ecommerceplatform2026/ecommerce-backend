using Application.DTOs.Product;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mappings
{
    public static class ProductMappingExtensions
    {
        public static ProductResponse ToProductResponse(this Product product)
        {
            var variants = product.ProductVariants?
                .Where(v => !v.IsDeleted)
                .Select(v => v.ToProductVariantResponse())
                .ToList() ?? new List<Application.DTOs.Product.ProductVariants.ProductVariantResponse>();

            var imageUrl = product.ProductImages?
                .Where(img => !img.IsDeleted)
                .OrderBy(image => image.CreatedAt)
                .FirstOrDefault()?.ImageUrl;

            var prices = variants.Select(variant => variant.Price).DefaultIfEmpty(product.BasePrice.Amount).ToList();
            var minPrice = prices.Min();
            var maxPrice = prices.Max();
            var totalStock = variants.Sum(variant => variant.Stock);
            var stockStatus = GetStockStatus(totalStock);

            var approvedReviews = product.Reviews?
                .Where(r => r.Status == ReviewStatus.Approved && !r.IsDeleted)
                .ToList() ?? new List<Review>();

            double averageRating = approvedReviews.Any() ? approvedReviews.Average(r => r.Rating) : 0.0;
            int reviewCount = approvedReviews.Count;

            return new ProductResponse(
                product.Id,
                product.CategoryId,
                product.Name,
                product.Description,
                product.Material,
                product.BasePrice.Amount,
                product.Status,
                product.Category?.Name,
                imageUrl,
                minPrice,
                maxPrice,
                totalStock,
                stockStatus,
                averageRating,
                reviewCount,
                variants);
        }

        public static ProductDetailResponse ToProductDetailResponse(this Product product)
        {
            var variants = product.ProductVariants?
                .Where(v => !v.IsDeleted)
                .OrderBy(variant => variant.SKU.Value)
                .Select(variant => new Application.DTOs.Product.ProductVariantResponse
                {
                    Id = variant.Id,
                    SKU = variant.SKU.Value,
                    Color = variant.Color,
                    Size = variant.Size,
                    Stock = variant.Stock,
                    StockStatus = GetStockStatus(variant.Stock),
                    Price = variant.Price.Amount
                })
                .ToList() ?? new List<Application.DTOs.Product.ProductVariantResponse>();

            var prices = variants.Select(variant => variant.Price).DefaultIfEmpty(product.BasePrice.Amount).ToList();
            var totalStock = variants.Sum(variant => variant.Stock);

            var approvedReviews = product.Reviews?
                .Where(r => r.Status == ReviewStatus.Approved && !r.IsDeleted)
                .ToList() ?? new List<Review>();

            double averageRating = approvedReviews.Any() ? approvedReviews.Average(r => r.Rating) : 0.0;
            int reviewCount = approvedReviews.Count;

            return new ProductDetailResponse
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Description = product.Description,
                Material = product.Material,
                BasePrice = product.BasePrice.Amount,
                Price = prices.Min(),
                MinPrice = prices.Min(),
                MaxPrice = prices.Max(),
                Status = product.Status,
                CategoryName = product.Category?.Name,
                TotalStock = totalStock,
                StockStatus = GetStockStatus(totalStock),
                AverageRating = averageRating,
                ReviewCount = reviewCount,
                Images = product.ProductImages?
                    .Where(img => !img.IsDeleted)
                    .OrderBy(image => image.CreatedAt)
                    .Select(image => new ProductImageResponse
                    {
                        Id = image.Id,
                        ImageUrl = image.ImageUrl
                    })
                    .ToList() ?? new List<ProductImageResponse>(),
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
                new Money(request.BasePrice, "VND"),
                request.Status);
        }

        public static void MapToEntity(this UpdateProductRequest request, Product product)
        {
            product.Update(
                request.CategoryId,
                request.Name,
                request.Description,
                request.Material,
                new Money(request.BasePrice, "VND"),
                request.Status);
        }

        private static string GetStockStatus(long stock)
        {
            return stock > 0 ? "InStock" : "OutOfStock";
        }
    }
}

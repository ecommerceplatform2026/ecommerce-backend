using Application.Common.Response;
using Application.DTOs.Product;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly EcommerceContext _context;
        private const string LikeEscape = "\\";

        public ProductRepository(EcommerceContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<PagedResult<Product>> GetProductsAsync(ProductListingRequest request, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<Product>()
                .AsNoTracking()
                .Where(product =>
                    !product.IsDeleted &&
                    product.Status == ProductStatus.Active &&
                    !product.Category.IsDeleted &&
                    product.Category.Status == CategoryStatus.Active);

            var search = Normalize(request.Search);
            if (search is not null)
            {
                var searchPattern = ToContainsPattern(search);
                query = query.Where(product =>
                    EF.Functions.ILike(product.Name, searchPattern, LikeEscape) ||
                    (product.Description != null && EF.Functions.ILike(product.Description, searchPattern, LikeEscape)) ||
                    (product.Material != null && EF.Functions.ILike(product.Material, searchPattern, LikeEscape)) ||
                    EF.Functions.ILike(product.Category.Name, searchPattern, LikeEscape));
            }

            if (request.CategoryId.HasValue)
                query = query.Where(product => product.CategoryId == request.CategoryId.Value);

            var category = Normalize(request.Category);
            if (category is not null)
            {
                var categoryPattern = ToContainsPattern(category);
                query = query.Where(product => EF.Functions.ILike(product.Category.Name, categoryPattern, LikeEscape));
            }

            if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
            {
                var minPrice = request.MinPrice;
                var maxPrice = request.MaxPrice;

                query = query.Where(product =>
                    ((!minPrice.HasValue || product.BasePrice.Amount >= minPrice.Value) &&
                     (!maxPrice.HasValue || product.BasePrice.Amount <= maxPrice.Value)) ||
                    product.ProductVariants.Any(variant =>
                        !variant.IsDeleted &&
                        (!minPrice.HasValue || variant.Price.Amount >= minPrice.Value) &&
                        (!maxPrice.HasValue || variant.Price.Amount <= maxPrice.Value)));
            }

            var size = Normalize(request.Size);
            if (size is not null)
            {
                query = query.Where(product => product.ProductVariants.Any(variant =>
                    !variant.IsDeleted &&
                    variant.Size != null &&
                    EF.Functions.ILike(variant.Size, EscapeLikePattern(size), LikeEscape)));
            }

            var color = Normalize(request.Color);
            if (color is not null)
            {
                query = query.Where(product => product.ProductVariants.Any(variant =>
                    !variant.IsDeleted &&
                    variant.Color != null &&
                    EF.Functions.ILike(variant.Color, EscapeLikePattern(color), LikeEscape)));
            }

            var material = Normalize(request.Material);
            if (material is not null)
                query = query.Where(product => product.Material != null && EF.Functions.ILike(product.Material, EscapeLikePattern(material), LikeEscape));

            var totalCount = await query.CountAsync(cancellationToken);

            var products = await ApplySorting(query, request.SortBy, request.SortDirection)
                .Include(product => product.Category)
                .Include(product => product.ProductVariants)
                .Include(product => product.ProductImages)
                .Include(product => product.Reviews)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Product>
            {
                Items = products,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy, string? sortDirection)
        {
            var descending = string.Equals(sortDirection?.Trim(), "desc", StringComparison.OrdinalIgnoreCase);

            return sortBy?.Trim().ToLowerInvariant() switch
            {
                "name" => descending
                    ? query.OrderByDescending(product => product.Name)
                    : query.OrderBy(product => product.Name),
                "price" or "baseprice" => descending
                    ? query.OrderByDescending(product => product.BasePrice)
                    : query.OrderBy(product => product.BasePrice),
                "category" => descending
                    ? query.OrderByDescending(product => product.Category.Name)
                    : query.OrderBy(product => product.Category.Name),
                "material" => descending
                    ? query.OrderByDescending(product => product.Material)
                    : query.OrderBy(product => product.Material),
                "createdat" or "created" => descending
                    ? query.OrderByDescending(product => product.CreatedAt)
                    : query.OrderBy(product => product.CreatedAt),
                _ => descending
                    ? query.OrderByDescending(product => product.CreatedAt)
                    : query.OrderBy(product => product.CreatedAt)
            };
        }

        private static string? Normalize(string? value)
        {
            var normalized = value?.Trim();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static string ToContainsPattern(string value)
        {
            return $"%{EscapeLikePattern(value)}%";
        }

        private static string EscapeLikePattern(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
        }
    }
}

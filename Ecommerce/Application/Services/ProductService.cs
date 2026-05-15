using Application.Common.Response;
using Application.DTOs.Product;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Application.Mappings;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public sealed class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<ProductResponse>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var product = await _unitOfWork.GetRepository<Product>().FindAsync(
                p => p.Id == id && !p.IsDeleted,
                includes: x => x.Category,
                cancellationToken: cancellationToken);

            if (product == null)
                return Result<ProductResponse>.NotFound("Product not found.");

            return Result<ProductResponse>.Success(product.ToProductResponse());
        }

        public async Task<Result<PagedResult<ProductResponse>>> GetProductsAsync(ProductListingRequest request, CancellationToken cancellationToken = default)
        {
            if (request.MinPrice < 0 || request.MaxPrice < 0)
                return Result<PagedResult<ProductResponse>>.Failure("Price range cannot contain negative values.");

            if (request.MinPrice.HasValue && request.MaxPrice.HasValue && request.MinPrice > request.MaxPrice)
                return Result<PagedResult<ProductResponse>>.Failure("Min price cannot be greater than max price.");

            var query = _unitOfWork.GetRepository<Product>()
                .GetQueryable()
                .AsNoTracking()
                .Where(product =>
                    !product.IsDeleted &&
                    product.Status == ProductStatus.Active &&
                    !product.Category.IsDeleted &&
                    product.Category.Status == CategoryStatus.Active);

            var search = Normalize(request.Search);
            if (search is not null)
            {
                query = query.Where(product =>
                    product.Name.ToLower().Contains(search) ||
                    (product.Description != null && product.Description.ToLower().Contains(search)) ||
                    (product.Material != null && product.Material.ToLower().Contains(search)) ||
                    product.Category.Name.ToLower().Contains(search));
            }

            if (request.CategoryId.HasValue)
                query = query.Where(product => product.CategoryId == request.CategoryId.Value);

            var category = Normalize(request.Category);
            if (category is not null)
                query = query.Where(product => product.Category.Name.ToLower().Contains(category));

            if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
            {
                var minPrice = request.MinPrice;
                var maxPrice = request.MaxPrice;

                query = query.Where(product =>
                    ((!minPrice.HasValue || product.BasePrice >= minPrice.Value) &&
                     (!maxPrice.HasValue || product.BasePrice <= maxPrice.Value)) ||
                    product.ProductVariants.Any(variant =>
                        !variant.IsDeleted &&
                        (!minPrice.HasValue || variant.Price >= minPrice.Value) &&
                        (!maxPrice.HasValue || variant.Price <= maxPrice.Value)));
            }

            var size = Normalize(request.Size);
            if (size is not null)
            {
                query = query.Where(product => product.ProductVariants.Any(variant =>
                    !variant.IsDeleted &&
                    variant.Size != null &&
                    variant.Size.ToLower() == size));
            }

            var color = Normalize(request.Color);
            if (color is not null)
            {
                query = query.Where(product => product.ProductVariants.Any(variant =>
                    !variant.IsDeleted &&
                    variant.Color != null &&
                    variant.Color.ToLower() == color));
            }

            var material = Normalize(request.Material);
            if (material is not null)
                query = query.Where(product => product.Material != null && product.Material.ToLower() == material);

            var totalCount = await query.CountAsync(cancellationToken);

            var products = await ApplySorting(query, request.SortBy, request.SortDirection)
                .Include(product => product.Category)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var result = new PagedResult<ProductResponse>
            {
                Items = products.Select(product => product.ToProductResponse()).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };

            return Result<PagedResult<ProductResponse>>.Success(result);
        }

        public async Task<Result<List<ProductResponse>>> GetAllProductsAsync(CancellationToken cancellationToken = default)
        {
            var products = await _unitOfWork.GetRepository<Product>().GetAllAsync(
                p => !p.IsDeleted,
                includes: x => x.Category,
                cancellationToken: cancellationToken);

            return Result<List<ProductResponse>>.Success(products.Select(p => p.ToProductResponse()).ToList());
        }

        public async Task<Result<ProductResponse>> CreateProductAsync(CreateProductRequest createProductRequest, CancellationToken cancellationToken = default)
        {
            var category = await _unitOfWork.GetRepository<Category>().FindAsync(c => c.Id == createProductRequest.CategoryId && !c.IsDeleted, cancellationToken: cancellationToken);
            if (category == null)
                return Result<ProductResponse>.NotFound("Category not found.");

            var product = createProductRequest.ToEntity();
            
            await _unitOfWork.GetRepository<Product>().AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return await GetProductByIdAsync(product.Id, cancellationToken);
        }

        public async Task<Result<ProductResponse>> UpdateProductAsync(Guid id, UpdateProductRequest updateProductRequest, CancellationToken cancellationToken = default)
        {
            var productRepository = _unitOfWork.GetRepository<Product>();
            var product = await productRepository.FindAsync(p => p.Id == id && !p.IsDeleted, asNoTracking: false, cancellationToken: cancellationToken);

            if (product == null)
                return Result<ProductResponse>.NotFound("Product not found.");

            if (product.CategoryId != updateProductRequest.CategoryId)
            {
                var category = await _unitOfWork.GetRepository<Category>().FindAsync(c => c.Id == updateProductRequest.CategoryId && !c.IsDeleted, cancellationToken: cancellationToken);
                if (category == null)
                    return Result<ProductResponse>.NotFound("Category not found.");
            }

            updateProductRequest.MapToEntity(product);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return await GetProductByIdAsync(id, cancellationToken);
        }

        public async Task<Result<bool>> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var productRepository = _unitOfWork.GetRepository<Product>();
            var product = await productRepository.FindAsync(p => p.Id == id && !p.IsDeleted, asNoTracking: false, cancellationToken: cancellationToken);

            if (product == null)
                return Result<bool>.NotFound("Product not found.");

            product.Deactivate();
            productRepository.Remove(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
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
            var normalized = value?.Trim().ToLowerInvariant();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }
    }
}

using Application.Common.Caching;
using Application.Common.Response;
using Application.DTOs.Product;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Application.Mappings;
using Domain.Entities;

namespace Application.Services
{
    public sealed class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        public ProductService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<Result<ProductResponse>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var cacheKey = CacheKeys.GetProductDetailKey(id);
            var response = await _cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var product = await _unitOfWork.GetRepository<Product>().FindAsync(
                        p => p.Id == id && !p.IsDeleted,
                        true,
                        cancellationToken,
                        x => x.Category,
                        x => x.ProductVariants);

                    return product?.ToProductResponse();
                },
                TimeSpan.FromHours(1),
                cancellationToken);

            if (response == null)
                return Result<ProductResponse>.NotFound("Product not found.");

            return Result<ProductResponse>.Success(response);
        }

        public async Task<Result<List<ProductResponse>>> GetAllProductsAsync(CancellationToken cancellationToken = default)
        {
            var response = await _cacheService.GetOrAddAsync(
                CacheKeys.ProductsAll,
                async () =>
                {
                    var products = await _unitOfWork.GetRepository<Product>().GetAllAsync(
                        p => !p.IsDeleted,
                        cancellationToken,
                        x => x.Category,
                        x => x.ProductVariants);

                    return products.Select(p => p.ToProductResponse()).ToList();
                },
                TimeSpan.FromHours(1),
                cancellationToken);

            return Result<List<ProductResponse>>.Success(response ?? new List<ProductResponse>());
        }

        public async Task<Result<ProductResponse>> CreateProductAsync(CreateProductRequest createProductRequest, CancellationToken cancellationToken = default)
        {
            var category = await _unitOfWork.GetRepository<Category>().FindAsync(c => c.Id == createProductRequest.CategoryId && !c.IsDeleted, cancellationToken: cancellationToken);
            if (category == null)
                return Result<ProductResponse>.NotFound("Category not found.");

            var product = createProductRequest.ToEntity();

            await _unitOfWork.GetRepository<Product>().AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cacheService.RemoveAsync(CacheKeys.ProductsAll, cancellationToken);

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

            await _cacheService.RemoveAsync(CacheKeys.ProductsAll, cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetProductDetailKey(id), cancellationToken);

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

            await _cacheService.RemoveAsync(CacheKeys.ProductsAll, cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetProductDetailKey(id), cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}

using Application.Common.Caching;
using Application.Common.Response;
using Application.DTOs.Product.ProductVariants;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Application.Mappings;
using Domain.Common;
using Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public sealed class ProductVariantService : IProductVariantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;

        public ProductVariantService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
        }

        private async Task InvalidateProductCacheAsync(Guid productId, CancellationToken cancellationToken)
        {
            await _cacheService.RemoveAsync(CacheKeys.ProductsAll, cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetProductDetailKey(productId), cancellationToken);
        }

        public async Task<Result<List<ProductVariantResponse>>> GetVariantsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var product = await _unitOfWork.GetRepository<Product>().FindAsync(
                p => p.Id == productId && !p.IsDeleted,
                true,
                cancellationToken,
                x => x.ProductVariants);

            if (product == null)
                return Result<List<ProductVariantResponse>>.NotFound("Product not found.");

            var variants = product.ProductVariants
                .Where(v => !v.IsDeleted)
                .Select(v => v.ToProductVariantResponse())
                .ToList();

            return Result<List<ProductVariantResponse>>.Success(variants);
        }

        public async Task<Result<ProductVariantResponse>> GetVariantByIdAsync(Guid productId, Guid variantId, CancellationToken cancellationToken = default)
        {
            var product = await _unitOfWork.GetRepository<Product>().FindAsync(
                p => p.Id == productId && !p.IsDeleted,
                true,
                cancellationToken,
                x => x.ProductVariants);

            if (product == null)
                return Result<ProductVariantResponse>.NotFound("Product not found.");

            var variant = product.ProductVariants.FirstOrDefault(v => v.Id == variantId && !v.IsDeleted);
            if (variant == null)
                return Result<ProductVariantResponse>.NotFound("Variant not found.");

            return Result<ProductVariantResponse>.Success(variant.ToProductVariantResponse());
        }

        public async Task<Result<ProductVariantResponse>> AddVariantAsync(Guid productId, CreateProductVariantRequest request, CancellationToken cancellationToken = default)
        {
            var product = await _unitOfWork.GetRepository<Product>().FindAsync(
                p => p.Id == productId && !p.IsDeleted,
                false,
                cancellationToken,
                x => x.ProductVariants);

            if (product == null)
                return Result<ProductVariantResponse>.NotFound("Product not found.");

            var normalizedSku = request.SKU?.Trim() ?? string.Empty;
            var skuExists = await _unitOfWork.GetRepository<ProductVariant>().TotalAsync(
                v => v.SKU.Value == normalizedSku.ToUpperInvariant() && !v.IsDeleted) > 0;

            if (skuExists)
                return Result<ProductVariantResponse>.Failure($"SKU '{normalizedSku}' is already in use by another product.");

            try
            {
                product.AddVariant(normalizedSku, request.Color, request.Size, request.Stock, new Money(request.Price, "VND"), request.LowStockThreshold);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await InvalidateProductCacheAsync(productId, cancellationToken);

                var newVariant = product.ProductVariants.First(v => v.SKU.Value.Equals(normalizedSku, StringComparison.OrdinalIgnoreCase) && !v.IsDeleted);
                return Result<ProductVariantResponse>.Success(newVariant.ToProductVariantResponse());
            }
            catch (InvalidOperationException ex)
            {
                return Result<ProductVariantResponse>.Failure(ex.Message);
            }
            catch (Exception)
            {
                return Result<ProductVariantResponse>.Failure($"SKU '{normalizedSku}' is already in use.");
            }
        }

        public async Task<Result<ProductVariantResponse>> UpdateVariantAsync(Guid productId, Guid variantId, UpdateProductVariantRequest request, CancellationToken cancellationToken = default)
        {
            var product = await _unitOfWork.GetRepository<Product>().FindAsync(
                p => p.Id == productId && !p.IsDeleted,
                false,
                cancellationToken,
                x => x.ProductVariants);

            if (product == null)
                return Result<ProductVariantResponse>.NotFound("Product not found.");

            var normalizedSku = request.SKU?.Trim() ?? string.Empty;
            var skuExists = await _unitOfWork.GetRepository<ProductVariant>().TotalAsync(
                v => v.Id != variantId && v.SKU.Value == normalizedSku.ToUpperInvariant() && !v.IsDeleted) > 0;

            if (skuExists)
                return Result<ProductVariantResponse>.Failure($"SKU '{normalizedSku}' is already in use by another product.");

            try
            {
                product.UpdateVariant(variantId, normalizedSku, request.Color, request.Size, request.Stock, new Money(request.Price, "VND"), request.LowStockThreshold);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await InvalidateProductCacheAsync(productId, cancellationToken);

                var updatedVariant = product.ProductVariants.First(v => v.Id == variantId);
                return Result<ProductVariantResponse>.Success(updatedVariant.ToProductVariantResponse());
            }
            catch (KeyNotFoundException)
            {
                return Result<ProductVariantResponse>.NotFound("Variant not found.");
            }
            catch (InvalidOperationException ex)
            {
                return Result<ProductVariantResponse>.Failure(ex.Message);
            }
            catch (Exception)
            {
                return Result<ProductVariantResponse>.Failure($"SKU '{normalizedSku}' is already in use.");
            }
        }

        public async Task<Result<bool>> UpdateStockAsync(Guid productId, Guid variantId, long newStock, CancellationToken cancellationToken = default)
        {
            var product = await _unitOfWork.GetRepository<Product>().FindAsync(
                p => p.Id == productId && !p.IsDeleted,
                false,
                cancellationToken,
                x => x.ProductVariants);

            if (product == null)
                return Result<bool>.NotFound("Product not found.");

            var variant = product.ProductVariants.FirstOrDefault(v => v.Id == variantId && !v.IsDeleted);
            if (variant == null)
                return Result<bool>.NotFound("Variant not found.");

            try
            {
                variant.UpdateStock(newStock);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await InvalidateProductCacheAsync(productId, cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (ArgumentException ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("An unexpected error occurred while updating the stock.");
            }
        }

        public async Task<Result<bool>> DeleteVariantAsync(Guid productId, Guid variantId, CancellationToken cancellationToken = default)
        {
            var product = await _unitOfWork.GetRepository<Product>().FindAsync(
                p => p.Id == productId && !p.IsDeleted,
                false,
                cancellationToken,
                x => x.ProductVariants);

            if (product == null)
                return Result<bool>.NotFound("Product not found.");

            var variant = product.ProductVariants.FirstOrDefault(v => v.Id == variantId && !v.IsDeleted);
            if (variant == null)
                return Result<bool>.NotFound("Variant not found.");

            var currentUserId = _currentUserService.GetUserIdOrNull() ?? "system";
            product.RemoveVariant(variantId, currentUserId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await InvalidateProductCacheAsync(productId, cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}

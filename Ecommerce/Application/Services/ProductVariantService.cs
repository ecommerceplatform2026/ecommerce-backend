using Application.Common.Response;
using Application.DTOs.Product.ProductVariant;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Application.Mappings;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public sealed class ProductVariantService : IProductVariantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public ProductVariantService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
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
            var skuExists = await _unitOfWork.GetRepository<ProductVariant>().GetQueryable().AnyAsync(
                v => v.SKU.ToLower() == normalizedSku.ToLower() && !v.IsDeleted,
                cancellationToken);

            if (skuExists)
                return Result<ProductVariantResponse>.Failure($"SKU '{normalizedSku}' is already in use by another product.");

            try
            {
                product.AddVariant(normalizedSku, request.Color, request.Size, request.Stock, request.Price, request.LowStockThreshold);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var newVariant = product.ProductVariants.First(v => v.SKU.Equals(normalizedSku, StringComparison.OrdinalIgnoreCase) && !v.IsDeleted);
                return Result<ProductVariantResponse>.Success(newVariant.ToProductVariantResponse());
            }
            catch (InvalidOperationException ex)
            {
                return Result<ProductVariantResponse>.Failure(ex.Message);
            }
            catch (DbUpdateException)
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
            var skuExists = await _unitOfWork.GetRepository<ProductVariant>().GetQueryable().AnyAsync(
                v => v.Id != variantId && v.SKU.ToLower() == normalizedSku.ToLower() && !v.IsDeleted,
                cancellationToken);

            if (skuExists)
                return Result<ProductVariantResponse>.Failure($"SKU '{normalizedSku}' is already in use by another product.");

            try
            {
                product.UpdateVariant(variantId, normalizedSku, request.Color, request.Size, request.Stock, request.Price, request.LowStockThreshold);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            catch (DbUpdateException)
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

            return Result<bool>.Success(true);
        }
    }
}

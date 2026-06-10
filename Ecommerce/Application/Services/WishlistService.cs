using Application.Common.Response;
using Application.DTOs.Wishlist;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public sealed class WishlistService : IWishlistService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public WishlistService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        private Result<Guid> GetCurrentUserId()
        {
            var userIdStr = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Result<Guid>.Unauthorized("User is not authenticated.");
            }
            return Result<Guid>.Success(userId);
        }

        public async Task<Result<List<WishlistItemResponse>>> GetWishlistAsync(List<Guid>? guestVariantIds = null, CancellationToken cancellationToken = default)
        {
            var userResult = GetCurrentUserId();
            if (!userResult.IsSuccess)
            {
                // Guest retrieval: Fetch variants for the provided IDs
                if (guestVariantIds == null || !guestVariantIds.Any())
                {
                    return Result<List<WishlistItemResponse>>.Success(new List<WishlistItemResponse>());
                }

                var guestItems = new List<WishlistItemResponse>();
                foreach (var variantId in guestVariantIds)
                {
                    var variant = await _unitOfWork.GetRepository<ProductVariant>()
                        .FindAsync(
                            pv => pv.Id == variantId,
                            true,
                            cancellationToken,
                            pv => pv.Product,
                            pv => pv.Product.ProductImages);

                    if (variant != null && variant.Product != null && variant.Product.Status != ProductStatus.Inactive)
                    {
                        guestItems.Add(MapVariantToResponse(Guid.Empty, variant));
                    }
                }

                return Result<List<WishlistItemResponse>>.Success(guestItems);
            }

            var userId = userResult.Value;

            var wishlistItems = await _unitOfWork.GetRepository<WishlistItem>()
                .GetAllAsync(
                    w => w.UserId == userId,
                    cancellationToken,
                    w => w.ProductVariant!,
                    w => w.ProductVariant!.Product!,
                    w => w.ProductVariant!.Product!.ProductImages);

            var response = wishlistItems
                .Where(w => w.ProductVariant != null && w.ProductVariant.Product != null)
                .Select(MapToResponse)
                .ToList();

            return Result<List<WishlistItemResponse>>.Success(response);
        }

        public async Task<Result<WishlistItemResponse>> AddToWishlistAsync(AddToWishlistRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Result<WishlistItemResponse>.Failure("Request cannot be null.");
            }

            var variant = await _unitOfWork.GetRepository<ProductVariant>()
                .FindAsync(
                    pv => pv.Id == request.ProductVariantId,
                    true,
                    cancellationToken,
                    pv => pv.Product,
                    pv => pv.Product.ProductImages);

            if (variant == null)
            {
                return Result<WishlistItemResponse>.NotFound("Product variant not found.");
            }

            if (variant.Product == null)
            {
                return Result<WishlistItemResponse>.NotFound("Product not found.");
            }

            if (variant.Product.Status == ProductStatus.Inactive)
            {
                return Result<WishlistItemResponse>.Failure("Product is inactive or unavailable.");
            }

            var userResult = GetCurrentUserId();
            if (!userResult.IsSuccess)
            {
                // Guest support: system prepares data for guest wishlist handling by returning variant details without DB save
                return Result<WishlistItemResponse>.Success(MapVariantToResponse(Guid.Empty, variant));
            }

            var userId = userResult.Value;

            var existingItem = await _unitOfWork.GetRepository<WishlistItem>()
                .FindAsync(
                    w => w.UserId == userId && w.ProductVariantId == request.ProductVariantId,
                    true,
                    cancellationToken);

            if (existingItem != null)
            {
                // Duplicate prevention: return success with existing item
                var savedItem = await _unitOfWork.GetRepository<WishlistItem>()
                    .FindAsync(
                        w => w.Id == existingItem.Id,
                        true,
                        cancellationToken,
                        w => w.ProductVariant!,
                        w => w.ProductVariant!.Product!,
                        w => w.ProductVariant!.Product!.ProductImages);

                if (savedItem != null)
                {
                    return Result<WishlistItemResponse>.Success(MapToResponse(savedItem));
                }
            }

            var newWishlistItem = WishlistItem.Create(userId, request.ProductVariantId);

            try
            {
                await _unitOfWork.GetRepository<WishlistItem>().AddAsync(newWishlistItem, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception)
            {
                // Handle concurrency or duplicate insertions from race conditions
                var itemFromDb = await _unitOfWork.GetRepository<WishlistItem>()
                    .FindAsync(
                        w => w.UserId == userId && w.ProductVariantId == request.ProductVariantId,
                        true,
                        cancellationToken,
                        w => w.ProductVariant!,
                        w => w.ProductVariant!.Product!,
                        w => w.ProductVariant!.Product!.ProductImages);

                if (itemFromDb != null)
                {
                    return Result<WishlistItemResponse>.Success(MapToResponse(itemFromDb));
                }

                throw;
            }

            var savedNewItem = await _unitOfWork.GetRepository<WishlistItem>()
                .FindAsync(
                    w => w.Id == newWishlistItem.Id,
                    true,
                    cancellationToken,
                    w => w.ProductVariant!,
                    w => w.ProductVariant!.Product!,
                    w => w.ProductVariant!.Product!.ProductImages);

            if (savedNewItem == null)
            {
                return Result<WishlistItemResponse>.Failure("Failed to retrieve the added wishlist item.");
            }

            return Result<WishlistItemResponse>.Success(MapToResponse(savedNewItem));
        }

        public async Task<Result<bool>> RemoveFromWishlistAsync(Guid variantId, CancellationToken cancellationToken = default)
        {
            var userResult = GetCurrentUserId();
            if (!userResult.IsSuccess)
            {
                // Guest support: guest removal returns success directly (client handles storage)
                return Result<bool>.Success(true);
            }

            var userId = userResult.Value;

            var wishlistItem = await _unitOfWork.GetRepository<WishlistItem>()
                .FindAsync(
                    w => w.UserId == userId && w.ProductVariantId == variantId,
                    false,
                    cancellationToken);

            if (wishlistItem == null)
            {
                return Result<bool>.NotFound("Wishlist item not found in wishlist.");
            }

            _unitOfWork.GetRepository<WishlistItem>().Remove(wishlistItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }

        public async Task<Result<List<WishlistItemResponse>>> MergeWishlistAsync(MergeWishlistRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Result<List<WishlistItemResponse>>.Failure("Request cannot be null.");
            }

            var userResult = GetCurrentUserId();
            if (!userResult.IsSuccess)
            {
                return Result<List<WishlistItemResponse>>.Unauthorized("Guest user cannot merge wishlist.");
            }

            var userId = userResult.Value;

            if (request.VariantIds == null || !request.VariantIds.Any())
            {
                return await GetWishlistAsync(null, cancellationToken);
            }

            var existingItems = await _unitOfWork.GetRepository<WishlistItem>()
                .GetAllAsync(w => w.UserId == userId, cancellationToken);

            var existingVariantIds = existingItems.Select(w => w.ProductVariantId).ToHashSet();

            foreach (var variantId in request.VariantIds)
            {
                if (existingVariantIds.Contains(variantId))
                {
                    continue;
                }

                var variant = await _unitOfWork.GetRepository<ProductVariant>()
                    .FindAsync(
                        pv => pv.Id == variantId,
                        true,
                        cancellationToken,
                        pv => pv.Product);

                if (variant == null || variant.Product == null || variant.Product.Status == ProductStatus.Inactive)
                {
                    continue;
                }

                var newItem = WishlistItem.Create(userId, variantId);
                await _unitOfWork.GetRepository<WishlistItem>().AddAsync(newItem, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return await GetWishlistAsync(null, cancellationToken);
        }

        private static WishlistItemResponse MapToResponse(WishlistItem item)
        {
            var variant = item.ProductVariant;
            return MapVariantToResponse(item.Id, variant!);
        }

        private static WishlistItemResponse MapVariantToResponse(Guid wishlistId, ProductVariant variant)
        {
            var product = variant.Product;
            var imageUrl = product?.ProductImages?.Where(img => !img.IsDeleted).OrderBy(img => img.CreatedAt).FirstOrDefault()?.ImageUrl;

            return new WishlistItemResponse(
                wishlistId,
                variant.Id,
                product?.Id ?? Guid.Empty,
                product?.Name ?? "Deleted Product",
                imageUrl,
                variant.SKU?.Value ?? "N/A",
                variant.Color,
                variant.Size,
                variant.Price?.Amount ?? 0,
                variant.Stock,
                variant.IsOutOfStock(),
                variant.IsLowStock(),
                variant.Stock > 0 ? "InStock" : "OutOfStock");
        }
    }
}

using Application.Common.Response;
using Application.DTOs.Cart;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public sealed class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public CartService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
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

        public async Task<Result<List<CartItemResponse>>> GetCartAsync(CancellationToken cancellationToken = default)
        {
            var userResult = GetCurrentUserId();
            if (!userResult.IsSuccess)
            {
                return Result<List<CartItemResponse>>.Unauthorized(userResult.Errors.FirstOrDefault() ?? "Unauthorized");
            }

            var userId = userResult.Value;

            var cartItems = await _unitOfWork.GetRepository<CartItem>()
                .GetQueryable()
                .Include(ci => ci.ProductVariant!)
                    .ThenInclude(pv => pv.Product!)
                        .ThenInclude(p => p.ProductImages!)
                .Where(ci => ci.UserId == userId)
                .ToListAsync(cancellationToken);

            var response = cartItems.Select(MapToResponse).ToList();
            return Result<List<CartItemResponse>>.Success(response);
        }

        public async Task<Result<CartItemResponse>> AddToCartAsync(AddToCartRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Result<CartItemResponse>.Failure("Request cannot be null.");
            }

            if (request.Quantity <= 0)
            {
                return Result<CartItemResponse>.Failure("Quantity must be greater than zero.");
            }

            var userResult = GetCurrentUserId();
            if (!userResult.IsSuccess)
            {
                return Result<CartItemResponse>.Unauthorized(userResult.Errors.FirstOrDefault() ?? "Unauthorized");
            }

            var userId = userResult.Value;

            var variant = await _unitOfWork.GetRepository<ProductVariant>()
                .GetQueryable()
                .Include(pv => pv.Product)
                .FirstOrDefaultAsync(pv => pv.Id == request.ProductVariantId, cancellationToken);

            if (variant == null)
            {
                return Result<CartItemResponse>.NotFound("Product variant not found.");
            }

            if (variant.Product == null || variant.Product.Status == ProductStatus.Inactive)
            {
                return Result<CartItemResponse>.Failure("Product is inactive or unavailable.");
            }

            var existingCartItem = await _unitOfWork.GetRepository<CartItem>()
                .FindAsync(ci => ci.UserId == userId && ci.ProductVariantId == request.ProductVariantId, asNoTracking: false, cancellationToken);

            var targetQuantity = request.Quantity + (existingCartItem?.Quantity ?? 0);

            if (variant.IsOutOfStock() || variant.Stock < targetQuantity)
            {
                return Result<CartItemResponse>.Failure($"Insufficient stock available. Maximum available stock is {variant.Stock}.");
            }

            if (existingCartItem != null)
            {
                existingCartItem.Quantity = targetQuantity;
                _unitOfWork.GetRepository<CartItem>().Update(existingCartItem);
            }
            else
            {
                var newCartItem = new CartItem
                {
                    UserId = userId,
                    ProductVariantId = request.ProductVariantId,
                    Quantity = request.Quantity
                };
                await _unitOfWork.GetRepository<CartItem>().AddAsync(newCartItem, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var savedItemId = existingCartItem?.Id ??
                (await _unitOfWork.GetRepository<CartItem>()
                    .FindAsync(ci => ci.UserId == userId && ci.ProductVariantId == request.ProductVariantId, asNoTracking: true, cancellationToken))?.Id;

            if (savedItemId == null)
            {
                return Result<CartItemResponse>.Failure("Failed to retrieve the updated cart item.");
            }

            var savedItem = await _unitOfWork.GetRepository<CartItem>()
                .GetQueryable()
                .Include(ci => ci.ProductVariant!)
                    .ThenInclude(pv => pv.Product!)
                        .ThenInclude(p => p.ProductImages!)
                .FirstOrDefaultAsync(ci => ci.Id == savedItemId.Value, cancellationToken);

            return Result<CartItemResponse>.Success(MapToResponse(savedItem!));
        }

        public async Task<Result<CartItemResponse>> UpdateCartItemAsync(Guid variantId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Result<CartItemResponse>.Failure("Request cannot be null.");
            }

            if (request.Quantity <= 0)
            {
                return Result<CartItemResponse>.Failure("Quantity must be greater than zero.");
            }

            var userResult = GetCurrentUserId();
            if (!userResult.IsSuccess)
            {
                return Result<CartItemResponse>.Unauthorized(userResult.Errors.FirstOrDefault() ?? "Unauthorized");
            }

            var userId = userResult.Value;

            var cartItem = await _unitOfWork.GetRepository<CartItem>()
                .FindAsync(ci => ci.UserId == userId && ci.ProductVariantId == variantId, asNoTracking: false, cancellationToken);

            if (cartItem == null)
            {
                return Result<CartItemResponse>.NotFound("Cart item not found.");
            }

            var variant = await _unitOfWork.GetRepository<ProductVariant>()
                .GetQueryable()
                .Include(pv => pv.Product)
                .FirstOrDefaultAsync(pv => pv.Id == variantId, cancellationToken);

            if (variant == null)
            {
                return Result<CartItemResponse>.NotFound("Product variant not found.");
            }

            if (variant.Product == null || variant.Product.Status == ProductStatus.Inactive)
            {
                return Result<CartItemResponse>.Failure("Product is inactive or unavailable.");
            }

            if (variant.IsOutOfStock() || variant.Stock < request.Quantity)
            {
                return Result<CartItemResponse>.Failure($"Insufficient stock available. Maximum available stock is {variant.Stock}.");
            }

            cartItem.Quantity = request.Quantity;
            _unitOfWork.GetRepository<CartItem>().Update(cartItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var savedItem = await _unitOfWork.GetRepository<CartItem>()
                .GetQueryable()
                .Include(ci => ci.ProductVariant!)
                    .ThenInclude(pv => pv.Product!)
                        .ThenInclude(p => p.ProductImages!)
                .FirstOrDefaultAsync(ci => ci.Id == cartItem.Id, cancellationToken);

            return Result<CartItemResponse>.Success(MapToResponse(savedItem!));
        }

        public async Task<Result<bool>> RemoveCartItemAsync(Guid variantId, CancellationToken cancellationToken = default)
        {
            var userResult = GetCurrentUserId();
            if (!userResult.IsSuccess)
            {
                return Result<bool>.Unauthorized(userResult.Errors.FirstOrDefault() ?? "Unauthorized");
            }

            var userId = userResult.Value;

            var cartItem = await _unitOfWork.GetRepository<CartItem>()
                .FindAsync(ci => ci.UserId == userId && ci.ProductVariantId == variantId, asNoTracking: false, cancellationToken);

            if (cartItem == null)
            {
                return Result<bool>.NotFound("Cart item not found in cart.");
            }

            _unitOfWork.GetRepository<CartItem>().Remove(cartItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }

        public async Task<Result<List<CartItemResponse>>> MergeCartAsync(MergeCartRequest request, CancellationToken cancellationToken = default)
        {
            var userResult = GetCurrentUserId();
            if (!userResult.IsSuccess)
            {
                return Result<List<CartItemResponse>>.Unauthorized(userResult.Errors.FirstOrDefault() ?? "Unauthorized");
            }

            var userId = userResult.Value;

            if (request?.Items == null || !request.Items.Any())
            {
                return await GetCartAsync(cancellationToken);
            }

            var existingCartItems = await _unitOfWork.GetRepository<CartItem>()
                .GetQueryable()
                .Where(ci => ci.UserId == userId)
                .ToListAsync(cancellationToken);

            var existingItemsDict = existingCartItems.ToDictionary(ci => ci.ProductVariantId);

            foreach (var guestItem in request.Items)
            {
                if (guestItem.Quantity <= 0) continue;

                var variant = await _unitOfWork.GetRepository<ProductVariant>()
                    .GetQueryable()
                    .Include(pv => pv.Product)
                    .FirstOrDefaultAsync(pv => pv.Id == guestItem.ProductVariantId, cancellationToken);

                if (variant == null || variant.Product == null || variant.Product.Status == ProductStatus.Inactive || variant.IsOutOfStock())
                {
                    continue;
                }

                if (existingItemsDict.TryGetValue(guestItem.ProductVariantId, out var existingItem))
                {
                    var targetQty = existingItem.Quantity + guestItem.Quantity;
                    if (targetQty > variant.Stock)
                    {
                        targetQty = (int)variant.Stock;
                    }

                    existingItem.Quantity = targetQty;
                    _unitOfWork.GetRepository<CartItem>().Update(existingItem);
                }
                else
                {
                    var targetQty = guestItem.Quantity;
                    if (targetQty > variant.Stock)
                    {
                        targetQty = (int)variant.Stock;
                    }

                    if (targetQty > 0)
                    {
                        var newCartItem = new CartItem
                        {
                            UserId = userId,
                            ProductVariantId = guestItem.ProductVariantId,
                            Quantity = targetQty
                        };
                        await _unitOfWork.GetRepository<CartItem>().AddAsync(newCartItem, cancellationToken);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return await GetCartAsync(cancellationToken);
        }

        private static CartItemResponse MapToResponse(CartItem item)
        {
            var variant = item.ProductVariant!;
            var product = variant.Product!;
            var imageUrl = product.ProductImages?.FirstOrDefault()?.ImageUrl;

            return new CartItemResponse(
                item.Id,
                item.ProductVariantId,
                product.Id,
                product.Name,
                imageUrl,
                variant.SKU,
                variant.Color,
                variant.Size,
                variant.Price,
                item.Quantity,
                variant.Stock,
                variant.IsLowStock(),
                variant.IsOutOfStock());
        }
    }
}

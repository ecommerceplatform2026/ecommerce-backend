using Application.Common.Response;
using Application.DTOs.Checkout;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application.Services
{
    public sealed class CheckoutService : ICheckoutService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public CheckoutService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<Result<CheckoutResponse>> ProcessCheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Result<CheckoutResponse>.Failure("Request cannot be null.");
            }

            if (!Enum.IsDefined(typeof(PaymentMethod), request.PaymentMethod))
            {
                return Result<CheckoutResponse>.Failure("Unsupported or invalid payment method.");
            }

            var userIdStr = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Result<CheckoutResponse>.Unauthorized("User is not authenticated.");
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var cartItems = await _unitOfWork.GetRepository<CartItem>()
                    .GetQueryable()
                    .Include(ci => ci.ProductVariant!)
                        .ThenInclude(pv => pv.Product!)
                    .Where(ci => ci.UserId == userId)
                    .ToListAsync(cancellationToken);

                if (!cartItems.Any())
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<CheckoutResponse>.Failure("Cart is empty.");
                }

                foreach (var cartItem in cartItems)
                {
                    var variant = cartItem.ProductVariant;
                    if (variant == null || variant.Product == null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Result<CheckoutResponse>.Failure("One or more items in the cart are invalid.");
                    }

                    if (variant.Product.Status == ProductStatus.Inactive)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Result<CheckoutResponse>.Failure($"Product '{variant.Product.Name}' is inactive or unavailable.");
                    }

                    if (variant.IsOutOfStock() || variant.Stock < cartItem.Quantity)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Result<CheckoutResponse>.Failure($"Insufficient stock for '{variant.Product.Name}' ({variant.SKU}). Available stock: {variant.Stock}, requested: {cartItem.Quantity}.");
                    }
                }

                int orderCode;
                bool codeExists;
                int attempts = 0;
                do
                {
                    orderCode = Random.Shared.Next(100000, 999999);
                    codeExists = await _unitOfWork.GetRepository<Order>()
                        .GetQueryable()
                        .AnyAsync(o => o.OrderCode == orderCode, cancellationToken);
                    attempts++;
                } while (codeExists && attempts < 10);

                if (codeExists)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<CheckoutResponse>.Failure("Unable to generate a unique order code. Please retry checkout.");
                }

                long totalAmount = cartItems.Sum(ci => ci.ProductVariant!.Price * ci.Quantity);

                var order = new Order
                {
                    UserId = userId,
                    TotalAmount = totalAmount,
                    Status = OrderStatus.Pending,
                    OrderCode = orderCode,
                    PaymentMethod = request.PaymentMethod
                };

                await _unitOfWork.GetRepository<Order>().AddAsync(order, cancellationToken);

                var orderItems = new List<OrderItem>();

                foreach (var cartItem in cartItems)
                {
                    var variant = cartItem.ProductVariant!;
                    var product = variant.Product!;

                    variant.UpdateStock(variant.Stock - cartItem.Quantity);

                    var snapshotObj = new
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        ProductDescription = product.Description,
                        Material = product.Material,
                        SKU = variant.SKU,
                        Color = variant.Color,
                        Size = variant.Size,
                        Price = variant.Price
                    };
                    var snapshotJson = JsonSerializer.Serialize(snapshotObj);

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductVariantId = cartItem.ProductVariantId,
                        Quantity = cartItem.Quantity,
                        Price = variant.Price,
                        ProductSnapshot = snapshotJson
                    };

                    await _unitOfWork.GetRepository<OrderItem>().AddAsync(orderItem, cancellationToken);
                    orderItems.Add(orderItem);

                    _unitOfWork.GetRepository<CartItem>().Remove(cartItem);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var itemResponses = orderItems.Select(oi => new CheckoutItemResponse(
                    oi.Id,
                    oi.ProductVariantId,
                    oi.Quantity,
                    oi.Price,
                    oi.ProductSnapshot)).ToList();

                var response = new CheckoutResponse(
                    order.Id,
                    order.OrderCode,
                    order.TotalAmount,
                    order.Status,
                    order.PaymentMethod,
                    itemResponses);

                return Result<CheckoutResponse>.Success(response);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<CheckoutResponse>.Failure("An error occurred during checkout. Please try again.");
            }
        }
    }
}

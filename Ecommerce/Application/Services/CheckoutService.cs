using Application.Common.Exceptions;
using Application.Common.Response;
using Application.DTOs.Checkout;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Common;
using System.Text.Json;

namespace Application.Services
{
    public sealed class CheckoutService : ICheckoutService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IVnPayService _vnPayService;

        public CheckoutService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IVnPayService vnPayService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _vnPayService = vnPayService ?? throw new ArgumentNullException(nameof(vnPayService));
        }

        public async Task<Result<CheckoutResponse>> ProcessCheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Result<CheckoutResponse>.Failure("Request cannot be null.");
            }

            var userIdStr = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Result<CheckoutResponse>.Unauthorized("User is not authenticated.");
            }

            const int maxRetryAttempts = 3;
            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                _unitOfWork.ClearTracker();

                var existingPayment = await _unitOfWork.GetRepository<Payment>()
                    .FindAsync(p => p.Order != null
                        && p.Order.UserId == userId
                        && p.Order.Status == OrderStatus.Pending
                        && p.Status == PaymentStatus.Pending
                        && (p.Order.PaymentMethod == PaymentMethod.VNPay
                            || p.Order.PaymentMethod == PaymentMethod.MoMo
                            || p.Order.PaymentMethod == PaymentMethod.ZaloPay
                            || p.Order.PaymentMethod == PaymentMethod.PayOS)
                        && !p.IsDeleted,
                        asNoTracking: false,
                        cancellationToken,
                        p => p.Order!,
                        p => p.Order!.OrderItems);

                if (existingPayment != null)
                {
                    var itemResponses = existingPayment.Order!.OrderItems.Select(oi => new CheckoutItemResponse(
                        oi.Id,
                        oi.ProductVariantId,
                        oi.Quantity,
                        oi.Price.Amount,
                        oi.ProductSnapshot)).ToList();

                    var response = new CheckoutResponse(
                        existingPayment.OrderId,
                        existingPayment.OrderCode,
                        existingPayment.Order.TotalAmount.Amount,
                        existingPayment.Order.Status,
                        existingPayment.Order.PaymentMethod,
                        itemResponses,
                        existingPayment.CheckoutUrl,
                        existingPayment.PaymentLinkId);

                    return Result<CheckoutResponse>.Success(response);
                }

                var cartItems = await _unitOfWork.GetRepository<CartItem>()
                    .GetAllTrackedAsync(
                        ci => ci.UserId == userId,
                        cancellationToken,
                        ci => ci.ProductVariant!,
                        ci => ci.ProductVariant!.Product!);

                if (cartItems == null || !cartItems.Any())
                {
                    return Result<CheckoutResponse>.Failure("Cart is empty.");
                }

                foreach (var cartItem in cartItems)
                {
                    var variant = cartItem.ProductVariant;
                    if (variant == null)
                    {
                        return Result<CheckoutResponse>.NotFound("One or more items in the cart are invalid.");
                    }

                    var product = variant.Product;
                    if (product == null)
                    {
                        return Result<CheckoutResponse>.NotFound("One or more items in the cart are invalid.");
                    }

                    if (product.Status == ProductStatus.Inactive)
                    {
                        return Result<CheckoutResponse>.Failure($"Product '{product.Name}' is inactive or unavailable.");
                    }

                    if (variant.IsOutOfStock() || variant.Stock < cartItem.Quantity)
                    {
                        return Result<CheckoutResponse>.Failure($"Insufficient stock for '{product.Name}' ({variant.SKU}). Available stock: {variant.Stock}, requested: {cartItem.Quantity}.");
                    }
                }

                await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    int orderCode;
                    var random = new Random();
                    bool codeExists;
                    int attempts = 0;
                    do
                    {
                        orderCode = random.Next(100000, 999999);
                        codeExists = (await _unitOfWork.GetRepository<Order>()
                            .TotalAsync(o => o.OrderCode == orderCode)) > 0;
                        attempts++;
                    } while (codeExists && attempts < 10);

                    long totalAmount = cartItems.Sum(ci => ci.ProductVariant!.Price.Amount * ci.Quantity);

                    var order = Order.Create(userId, orderCode, request.PaymentMethod);

                    var productIdsToInvalidate = new List<Guid>();

                    foreach (var cartItem in cartItems)
                    {
                        var variant = cartItem.ProductVariant!;
                        var product = variant.Product!;

                        variant.UpdateStock(variant.Stock - cartItem.Quantity);
                        _unitOfWork.GetRepository<ProductVariant>().Update(variant);

                        if (!productIdsToInvalidate.Contains(product.Id))
                        {
                            productIdsToInvalidate.Add(product.Id);
                        }

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

                        order.AddItem(cartItem.ProductVariantId, cartItem.Quantity, variant.Price, snapshotJson);

                        _unitOfWork.GetRepository<CartItem>().Remove(cartItem);
                    }

                    await _unitOfWork.GetRepository<Order>().AddAsync(order, cancellationToken);

                    string? checkoutUrl = null;
                    string paymentLinkId = "";

                    if (request.PaymentMethod == PaymentMethod.VNPay)
                    {
                        paymentLinkId = Guid.NewGuid().ToString();
                        checkoutUrl = _vnPayService.CreatePaymentUrl(orderCode, totalAmount);
                    }
                    else if (request.PaymentMethod == PaymentMethod.MoMo || request.PaymentMethod == PaymentMethod.ZaloPay || request.PaymentMethod == PaymentMethod.PayOS)
                    {
                        paymentLinkId = Guid.NewGuid().ToString();
                        checkoutUrl = $"https://payment-gateway.mock/pay/{orderCode}";
                    }

                    var payment = Payment.Create(order.Id, orderCode, order.TotalAmount, paymentLinkId, checkoutUrl);

                    await _unitOfWork.GetRepository<Payment>().AddAsync(payment, cancellationToken);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    var itemResponses = order.OrderItems.Select(oi => new CheckoutItemResponse(
                        oi.Id,
                        oi.ProductVariantId,
                        oi.Quantity,
                        oi.Price.Amount,
                        oi.ProductSnapshot)).ToList();

                    var response = new CheckoutResponse(
                        order.Id,
                        order.OrderCode,
                        order.TotalAmount.Amount,
                        order.Status,
                        order.PaymentMethod,
                        itemResponses,
                        checkoutUrl,
                        paymentLinkId);

                    return Result<CheckoutResponse>.Success(response);
                }
                catch (ConcurrencyException)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    if (attempt == maxRetryAttempts)
                    {
                        return Result<CheckoutResponse>.Failure("Checkout failed due to concurrent update conflicts. Please try again.");
                    }
                    await Task.Delay(100 * attempt, cancellationToken);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<CheckoutResponse>.Failure($"An error occurred during checkout: {ex.Message}");
                }
            }

            return Result<CheckoutResponse>.Failure("Checkout failed.");
        }
    }
}

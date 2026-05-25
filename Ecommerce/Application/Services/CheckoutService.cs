using Application.Common.Response;
using Application.Configurations;
using Application.DTOs.Checkout;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public sealed class CheckoutService : ICheckoutService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly VnPaySettings _vnPaySettings;

        public CheckoutService(
            IUnitOfWork unitOfWork, 
            ICurrentUserService currentUserService,
            IOptions<VnPaySettings> vnPayOptions)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _vnPaySettings = vnPayOptions?.Value ?? throw new ArgumentNullException(nameof(vnPayOptions));
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

            var existingPayment = await _unitOfWork.GetRepository<Payment>()
                .GetQueryable()
                .Include(p => p.Order)
                    .ThenInclude(o => o!.OrderItems)
                .FirstOrDefaultAsync(p => p.Order != null 
                    && p.Order.UserId == userId 
                    && p.Order.Status == OrderStatus.Pending 
                    && p.Status == PaymentStatus.Pending
                    && (p.Order.PaymentMethod == PaymentMethod.VNPay 
                        || p.Order.PaymentMethod == PaymentMethod.MoMo 
                        || p.Order.PaymentMethod == PaymentMethod.ZaloPay 
                        || p.Order.PaymentMethod == PaymentMethod.PayOS)
                    && !p.IsDeleted, cancellationToken);

            if (existingPayment != null)
            {
                var itemResponses = existingPayment.Order!.OrderItems.Select(oi => new CheckoutItemResponse(
                    oi.Id,
                    oi.ProductVariantId,
                    oi.Quantity,
                    oi.Price,
                    oi.ProductSnapshot)).ToList();

                var response = new CheckoutResponse(
                    existingPayment.OrderId,
                    existingPayment.OrderCode,
                    existingPayment.Order.TotalAmount,
                    existingPayment.Order.Status,
                    existingPayment.Order.PaymentMethod,
                    itemResponses,
                    existingPayment.CheckoutUrl,
                    existingPayment.PaymentLinkId);

                return Result<CheckoutResponse>.Success(response);
            }

            var cartItems = await _unitOfWork.GetRepository<CartItem>()
                .GetQueryable()
                .Include(ci => ci.ProductVariant!)
                    .ThenInclude(pv => pv.Product!)
                .Where(ci => ci.UserId == userId)
                .ToListAsync(cancellationToken);

            if (cartItems == null || !cartItems.Any())
            {
                return Result<CheckoutResponse>.Failure("Cart is empty.");
            }

            foreach (var cartItem in cartItems)
            {
                var variant = cartItem.ProductVariant;
                if (variant == null)
                {
                    return Result<CheckoutResponse>.Failure("One or more items in the cart are invalid.");
                }

                var product = variant.Product;
                if (product == null)
                {
                    return Result<CheckoutResponse>.Failure("One or more items in the cart are invalid.");
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

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                int orderCode;
                var random = new Random();
                bool codeExists;
                int attempts = 0;
                do
                {
                    orderCode = random.Next(100000, 999999);
                    codeExists = await _unitOfWork.GetRepository<Order>()
                        .GetQueryable()
                        .AnyAsync(o => o.OrderCode == orderCode, cancellationToken);
                    attempts++;
                } while (codeExists && attempts < 10);

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
                    _unitOfWork.GetRepository<ProductVariant>().Update(variant);

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

                string? checkoutUrl = null;
                string paymentLinkId = "";

                if (request.PaymentMethod == PaymentMethod.VNPay)
                {
                    paymentLinkId = Guid.NewGuid().ToString();
                    var vnPay = new VnPayLibrary();
                    vnPay.AddRequestData("vnp_Version", "2.1.0");
                    vnPay.AddRequestData("vnp_Command", "pay");
                    vnPay.AddRequestData("vnp_TmnCode", _vnPaySettings.TmnCode);
                    vnPay.AddRequestData("vnp_Amount", (totalAmount * 100).ToString());
                    vnPay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                    vnPay.AddRequestData("vnp_CurrCode", "VND");
                    vnPay.AddRequestData("vnp_IpAddr", "127.0.0.1");
                    vnPay.AddRequestData("vnp_Locale", "vn");
                    vnPay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {orderCode}");
                    vnPay.AddRequestData("vnp_OrderType", "other");
                    vnPay.AddRequestData("vnp_ReturnUrl", _vnPaySettings.ReturnUrl);
                    vnPay.AddRequestData("vnp_TxnRef", orderCode.ToString());

                    checkoutUrl = vnPay.CreateRequestUrl(_vnPaySettings.PaymentUrl, _vnPaySettings.HashSecret);
                }
                else if (request.PaymentMethod == PaymentMethod.MoMo || request.PaymentMethod == PaymentMethod.ZaloPay || request.PaymentMethod == PaymentMethod.PayOS)
                {
                    paymentLinkId = Guid.NewGuid().ToString();
                    checkoutUrl = $"https://payment-gateway.mock/pay/{orderCode}";
                }

                var payment = new Payment
                {
                    OrderId = order.Id,
                    PaymentLinkId = paymentLinkId,
                    OrderCode = orderCode,
                    CheckoutUrl = checkoutUrl,
                    Amount = totalAmount,
                    Currency = "VND",
                    Status = PaymentStatus.Pending
                };

                await _unitOfWork.GetRepository<Payment>().AddAsync(payment, cancellationToken);

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
                    itemResponses,
                    checkoutUrl,
                    paymentLinkId);

                return Result<CheckoutResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return Result<CheckoutResponse>.Failure($"An error occurred during checkout: {ex.Message}");
            }
        }
    }
}

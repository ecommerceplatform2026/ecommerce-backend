using Application.Common.Caching;
using Application.Common.Response;
using Application.DTOs.Payment;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public sealed class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVnPayService _vnPayService;

        public PaymentService(
            IUnitOfWork unitOfWork,
            IVnPayService vnPayService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _vnPayService = vnPayService ?? throw new ArgumentNullException(nameof(vnPayService));
        }

        public async Task<Result<PaymentResponse>> ProcessVnPayCallbackAsync(IDictionary<string, string> queryParameters, CancellationToken cancellationToken = default)
        {
            if (queryParameters == null || !queryParameters.Any())
            {
                return Result<PaymentResponse>.Failure("Invalid query parameters.");
            }

            var isValidCallback = _vnPayService.ValidateCallback(queryParameters, out var orderCode, out var isSuccess);
            if (!isValidCallback)
            {
                return Result<PaymentResponse>.Failure("Invalid callback data or signature.");
            }

            var paymentRecord = await _unitOfWork.GetRepository<Payment>()
                .FindAsync(
                    p => p.OrderCode == orderCode,
                    asNoTracking: false,
                    cancellationToken,
                    p => p.Order!,
                    p => p.Order!.OrderItems);

            if (paymentRecord == null)
            {
                return Result<PaymentResponse>.NotFound($"Payment for order code {orderCode} not found.");
            }

            if (paymentRecord.Status != PaymentStatus.Pending)
            {
                var response = new PaymentResponse(
                    paymentRecord.Id,
                    paymentRecord.OrderId,
                    paymentRecord.OrderCode,
                    paymentRecord.Amount.Amount,
                    paymentRecord.Status,
                    paymentRecord.PaymentLinkId,
                    paymentRecord.CheckoutUrl);

                return paymentRecord.Status == PaymentStatus.Success
                    ? Result<PaymentResponse>.Success(response)
                    : Result<PaymentResponse>.Failure("Payment was already processed as failed.");
            }

            if (isSuccess)
            {
                paymentRecord.Complete(DateTime.UtcNow);

                if (paymentRecord.Order != null)
                {
                    paymentRecord.Order.ConfirmPayment();
                    _unitOfWork.GetRepository<Order>().Update(paymentRecord.Order);
                }
            }
            else
            {
                paymentRecord.Fail();
                if (paymentRecord.Order != null)
                {
                    paymentRecord.Order.Cancel();
                    _unitOfWork.GetRepository<Order>().Update(paymentRecord.Order);

                    foreach (var orderItem in paymentRecord.Order.OrderItems)
                    {
                        var variant = await _unitOfWork.GetRepository<ProductVariant>()
                            .FindAsync(pv => pv.Id == orderItem.ProductVariantId, asNoTracking: false, cancellationToken);

                        if (variant != null)
                        {
                            variant.UpdateStock(variant.Stock + orderItem.Quantity);
                            _unitOfWork.GetRepository<ProductVariant>().Update(variant);
                        }

                        var cartItem = new CartItem
                        {
                            UserId = paymentRecord.Order.UserId,
                            ProductVariantId = orderItem.ProductVariantId,
                            Quantity = orderItem.Quantity
                        };
                        await _unitOfWork.GetRepository<CartItem>().AddAsync(cartItem, cancellationToken);
                    }
                }
            }

            _unitOfWork.GetRepository<Payment>().Update(paymentRecord);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var responseDto = new PaymentResponse(
                paymentRecord.Id,
                paymentRecord.OrderId,
                paymentRecord.OrderCode,
                paymentRecord.Amount.Amount,
                paymentRecord.Status,
                paymentRecord.PaymentLinkId,
                paymentRecord.CheckoutUrl);

            if (isSuccess)
            {
                return Result<PaymentResponse>.Success(responseDto);
            }

            return Result<PaymentResponse>.Failure("Payment failed.");
        }
    }
}

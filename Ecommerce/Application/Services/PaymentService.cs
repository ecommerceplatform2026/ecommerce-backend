using Application.Common.Response;
using Application.Configurations;
using Application.DTOs.Payment;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public sealed class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly VnPaySettings _vnPaySettings;

        public PaymentService(IUnitOfWork unitOfWork, IOptions<VnPaySettings> vnPayOptions)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _vnPaySettings = vnPayOptions?.Value ?? throw new ArgumentNullException(nameof(vnPayOptions));
        }

        public async Task<Result<PaymentResponse>> ProcessVnPayCallbackAsync(IDictionary<string, string> queryParameters, CancellationToken cancellationToken = default)
        {
            if (queryParameters == null || !queryParameters.Any())
            {
                return Result<PaymentResponse>.Failure("Invalid query parameters.");
            }

            var vnPay = new VnPayLibrary();
            foreach (var kv in queryParameters)
            {
                if (!string.IsNullOrEmpty(kv.Key) && kv.Key.StartsWith("vnp_"))
                {
                    vnPay.AddResponseData(kv.Key, kv.Value);
                }
            }

            var vnpSecureHash = queryParameters.TryGetValue("vnp_SecureHash", out var secureHash) ? secureHash : string.Empty;
            if (string.IsNullOrEmpty(vnpSecureHash))
            {
                return Result<PaymentResponse>.Failure("Missing secure hash.");
            }

            var isValidSignature = vnPay.ValidateSignature(vnpSecureHash, _vnPaySettings.HashSecret);
            if (!isValidSignature)
            {
                return Result<PaymentResponse>.Failure("Invalid signature.");
            }

            var txnRef = vnPay.GetResponseData("vnp_TxnRef");
            if (!int.TryParse(txnRef, out var orderCode))
            {
                return Result<PaymentResponse>.Failure("Invalid transaction reference.");
            }

            var responseCode = vnPay.GetResponseData("vnp_ResponseCode");
            var isSuccess = responseCode == "00";

            var paymentRecord = await _unitOfWork.GetRepository<Payment>()
                .GetQueryable()
                .Include(p => p.Order)
                    .ThenInclude(o => o!.OrderItems)
                .FirstOrDefaultAsync(p => p.OrderCode == orderCode, cancellationToken);

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
                    paymentRecord.Amount,
                    paymentRecord.Status,
                    paymentRecord.PaymentLinkId,
                    paymentRecord.CheckoutUrl);

                return paymentRecord.Status == PaymentStatus.Success
                    ? Result<PaymentResponse>.Success(response)
                    : Result<PaymentResponse>.Failure("Payment was already processed as failed.");
            }

            if (isSuccess)
            {
                paymentRecord.Status = PaymentStatus.Success;
                paymentRecord.PaidAt = DateTime.UtcNow;
                
                if (paymentRecord.Order != null)
                {
                    paymentRecord.Order.Status = OrderStatus.Confirmed;
                    _unitOfWork.GetRepository<Order>().Update(paymentRecord.Order);
                }
            }
            else
            {
                paymentRecord.Status = PaymentStatus.Failed;
                if (paymentRecord.Order != null)
                {
                    paymentRecord.Order.Status = OrderStatus.Cancelled;
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
                paymentRecord.Amount,
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

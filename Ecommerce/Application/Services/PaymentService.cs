using Application.Common.Caching;
using Application.Common.Response;
using Application.DTOs.Payment;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
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
        private readonly IVnPayService _vnPayService;
        private readonly IMomoService _momoService;
        private readonly IZaloPayService _zaloPayService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IUnitOfWork unitOfWork,
            IVnPayService vnPayService,
            IMomoService momoService,
            IZaloPayService zaloPayService,
            ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _vnPayService = vnPayService ?? throw new ArgumentNullException(nameof(vnPayService));
            _momoService = momoService ?? throw new ArgumentNullException(nameof(momoService));
            _zaloPayService = zaloPayService ?? throw new ArgumentNullException(nameof(zaloPayService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<PaymentResponse>> ProcessVnPayCallbackAsync(IDictionary<string, string> queryParameters, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("VNPay callback invoked with {ParamCount} parameters", queryParameters?.Count ?? 0);
            if (queryParameters == null || !queryParameters.Any())
            {
                return Result<PaymentResponse>.Failure("Invalid query parameters.");
            }

            var isValidCallback = _vnPayService.ValidateCallback(queryParameters, out var orderCode, out var isSuccess);
            if (!isValidCallback)
            {
                return Result<PaymentResponse>.Failure("Invalid callback data or signature.");
            }

            var paymentRecord = await GetPaymentRecordAsync(orderCode, cancellationToken);
            if (paymentRecord == null)
            {
                return Result<PaymentResponse>.NotFound($"Payment for order code {orderCode} not found.");
            }

            if (paymentRecord.Status != PaymentStatus.Pending)
            {
                return GetProcessedResponse(paymentRecord);
            }

            return await CompleteCallbackAsync(paymentRecord, isSuccess, cancellationToken);
        }

        public async Task<Result<PaymentResponse>> ProcessMomoCallbackAsync(IDictionary<string, string> queryParameters, CancellationToken cancellationToken = default)
        {
            if (queryParameters == null || !queryParameters.Any())
            {
                return Result<PaymentResponse>.Failure("Invalid query parameters.");
            }

            var isValidCallback = _momoService.ValidateCallback(queryParameters, out var orderCode, out var isSuccess);
            if (!isValidCallback)
            {
                return Result<PaymentResponse>.Failure("Invalid callback data or signature.");
            }

            var paymentRecord = await GetPaymentRecordAsync(orderCode, cancellationToken);
            if (paymentRecord == null)
            {
                return Result<PaymentResponse>.NotFound($"Payment for order code {orderCode} not found.");
            }

            if (paymentRecord.Status != PaymentStatus.Pending)
            {
                return GetProcessedResponse(paymentRecord);
            }

            return await CompleteCallbackAsync(paymentRecord, isSuccess, cancellationToken);
        }

        public async Task<Result<PaymentResponse>> ProcessZaloPayCallbackAsync(IDictionary<string, string> queryParameters, CancellationToken cancellationToken = default)
        {
            if (queryParameters == null || !queryParameters.Any())
            {
                return Result<PaymentResponse>.Failure("Invalid query parameters.");
            }

            var isValidCallback = _zaloPayService.ValidateCallback(queryParameters, out var orderCode, out var isSuccess);
            if (!isValidCallback)
            {
                return Result<PaymentResponse>.Failure("Invalid callback data or signature.");
            }

            var paymentRecord = await GetPaymentRecordAsync(orderCode, cancellationToken);
            if (paymentRecord == null)
            {
                return Result<PaymentResponse>.NotFound($"Payment for order code {orderCode} not found.");
            }

            if (paymentRecord.Status != PaymentStatus.Pending)
            {
                return GetProcessedResponse(paymentRecord);
            }

            return await CompleteCallbackAsync(paymentRecord, isSuccess, cancellationToken);
        }

        public async Task<Result<PaymentResponse>> GetPaymentStatusAsync(int orderCode, CancellationToken cancellationToken = default)
        {
            var paymentRecord = await GetPaymentRecordAsync(orderCode, cancellationToken);
            if (paymentRecord == null)
            {
                return Result<PaymentResponse>.NotFound($"Payment for order code {orderCode} not found.");
            }

            var response = new PaymentResponse(
                paymentRecord.Id,
                paymentRecord.OrderId,
                paymentRecord.OrderCode,
                paymentRecord.Amount.Amount,
                paymentRecord.Status,
                paymentRecord.PaymentLinkId,
                paymentRecord.CheckoutUrl);

            return Result<PaymentResponse>.Success(response);
        }

        private async Task<Payment?> GetPaymentRecordAsync(int orderCode, CancellationToken cancellationToken)
        {
            return await _unitOfWork.GetRepository<Payment>()
                .FindAsyncWithStringIncludes(
                    p => p.OrderCode == orderCode,
                    asNoTracking: false,
                    cancellationToken,
                    "Order",
                    "Order.OrderItems",
                    "Order.OrderItems.ProductVariant");
        }

        private Result<PaymentResponse> GetProcessedResponse(Payment paymentRecord)
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

        private async Task<Result<PaymentResponse>> CompleteCallbackAsync(Payment paymentRecord, bool isSuccess, CancellationToken cancellationToken)
        {
            if (isSuccess)
            {
                paymentRecord.Complete(DateTime.UtcNow);

                if (paymentRecord.Order != null)
                {
                    paymentRecord.Order.MarkAsConfirmed();
                    _unitOfWork.GetRepository<Order>().Update(paymentRecord.Order);
                }
            }
            else
            {
                paymentRecord.Fail();
                if (paymentRecord.Order != null)
                {
                    paymentRecord.Order.MarkAsCancelled();
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

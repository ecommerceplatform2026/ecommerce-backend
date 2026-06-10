using Application.Configurations;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using VNPAY;
using VNPAY.Models;
using VNPAY.Models.Enums;
using VNPAY.Models.Exceptions;

namespace Infrastructure.Services
{
    public sealed class VnPayService : IVnPayService
    {
        private readonly IVnpayClient _vnpayClient;
        private readonly VnPaySettings _vnPaySettings;

        public VnPayService(IVnpayClient vnpayClient, IOptions<VnPaySettings> vnPayOptions)
        {
            _vnpayClient = vnpayClient ?? throw new ArgumentNullException(nameof(vnpayClient));
            _vnPaySettings = vnPayOptions?.Value ?? throw new ArgumentNullException(nameof(vnPayOptions));
        }

        public string CreatePaymentUrl(int orderCode, long totalAmount)
        {
            var request = new VnpayPaymentRequest
            {
                Money = totalAmount,
                Description = $"Thanh toan don hang {orderCode}",
                BankCode = BankCode.ANY,
                Language = DisplayLanguage.Vietnamese
            };

            // Set the read-only / internal PaymentId property using Reflection
            var prop = typeof(VnpayPaymentRequest).GetProperty("PaymentId");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(request, (long)orderCode);
            }

            var paymentUrlInfo = _vnpayClient.CreatePaymentUrl(request);
            return paymentUrlInfo.Url;
        }

        public bool ValidateCallback(IDictionary<string, string> queryParameters, out int orderCode, out bool isSuccess)
        {
            orderCode = 0;
            isSuccess = false;

            if (queryParameters == null || !queryParameters.Any())
            {
                return false;
            }

            try
            {
                var queryCollection = new QueryCollection(
                    queryParameters.ToDictionary(
                        x => x.Key,
                        x => new StringValues(x.Value)
                    )
                );

                var paymentResult = _vnpayClient.GetPaymentResult(queryCollection);
                orderCode = (int)paymentResult.PaymentId;
                isSuccess = true;
                return true;
            }
            catch (VnpayException ex)
            {
                if (queryParameters.TryGetValue("vnp_TxnRef", out var txnRefStr) && int.TryParse(txnRefStr, out var parsedOrderCode))
                {
                    orderCode = parsedOrderCode;
                }

                if (ex.Message != null && (ex.Message.Contains("chữ ký") || ex.Message.Contains("signature") || ex.Message.Contains("hash") || ex.Message.Contains("checksum")))
                {
                    return false;
                }

                if (ex.PaymentResponseCode != PaymentResponseCode.Code_00)
                {
                    isSuccess = false;
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

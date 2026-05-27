using Application.Configurations;
using Application.Interfaces.Services;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Infrastructure.Services
{
    public sealed class VnPayService : IVnPayService
    {
        private readonly VnPaySettings _vnPaySettings;

        public VnPayService(IOptions<VnPaySettings> vnPayOptions)
        {
            _vnPaySettings = vnPayOptions?.Value ?? throw new ArgumentNullException(nameof(vnPayOptions));
        }

        public string CreatePaymentUrl(int orderCode, long totalAmount)
        {
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

            return vnPay.CreateRequestUrl(_vnPaySettings.PaymentUrl, _vnPaySettings.HashSecret);
        }

        public bool ValidateCallback(IDictionary<string, string> queryParameters, out int orderCode, out bool isSuccess)
        {
            orderCode = 0;
            isSuccess = false;

            if (queryParameters == null || !queryParameters.Any())
            {
                return false;
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
                return false;
            }

            var isValidSignature = vnPay.ValidateSignature(vnpSecureHash, _vnPaySettings.HashSecret);
            if (!isValidSignature)
            {
                return false;
            }

            var txnRef = vnPay.GetResponseData("vnp_TxnRef");
            if (!int.TryParse(txnRef, out orderCode))
            {
                return false;
            }

            var responseCode = vnPay.GetResponseData("vnp_ResponseCode");
            isSuccess = responseCode == "00";

            return true;
        }
    }
}

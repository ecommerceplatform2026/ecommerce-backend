using Application.Configurations;
using Application.Interfaces.Services;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public sealed class MomoService : IMomoService
    {
        private readonly HttpClient _httpClient;
        private readonly MomoSettings _settings;

        public MomoService(HttpClient httpClient, IOptions<MomoSettings> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<string> CreatePaymentUrlAsync(int orderCode, long totalAmount, CancellationToken cancellationToken = default)
        {
            var requestId = Guid.NewGuid().ToString();
            var orderId = orderCode.ToString() + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var orderInfo = $"Thanh toan don hang {orderCode}";
            var redirectUrl = $"http://localhost:3000/payments/momo/callback"; // Frontend callback URL
            var ipnUrl = $"https://example.com/api/payments/momo/callback";     // Backend IPN
            var requestType = "captureWallet";
            var extraData = "";

            // rawSignature: accessKey=$accessKey&amount=$amount&extraData=$extraData&ipnUrl=$ipnUrl&orderId=$orderId&orderInfo=$orderInfo&partnerCode=$partnerCode&redirectUrl=$redirectUrl&requestId=$requestId&requestType=$requestType
            var rawSignature = $"accessKey={_settings.AccessKey}&amount={totalAmount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={_settings.PartnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
            var signature = HmacSha256(_settings.SecretKey, rawSignature);

            var requestBody = new
            {
                partnerCode = _settings.PartnerCode,
                partnerName = "Test",
                storeId = "MomoTestStore",
                requestId,
                amount = totalAmount,
                orderId,
                orderInfo,
                redirectUrl,
                ipnUrl,
                lang = "vi",
                extraData,
                requestType,
                signature
            };

            var response = await _httpClient.PostAsJsonAsync(_settings.CreateUrl, requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"MoMo API returned {(int)response.StatusCode}: {errorBody}");
            }

            var resData = await response.Content.ReadFromJsonAsync<MomoCreateResponse>(cancellationToken: cancellationToken);

            if (resData == null || resData.ResultCode != 0)
            {
                var errorMsg = resData?.Message ?? "Unknown error";
                throw new InvalidOperationException($"MoMo payment creation failed: {errorMsg}");
            }

            return resData.PayUrl;
        }

        public bool ValidateCallback(IDictionary<string, string> parameters, out int orderCode, out bool isSuccess)
        {
            orderCode = 0;
            isSuccess = false;

            if (parameters == null || !parameters.TryGetValue("signature", out var signature))
            {
                return false;
            }

            parameters.TryGetValue("partnerCode", out var partnerCode);
            parameters.TryGetValue("orderId", out var orderIdStr);
            parameters.TryGetValue("requestId", out var requestId);
            parameters.TryGetValue("amount", out var amount);
            parameters.TryGetValue("orderInfo", out var orderInfo);
            parameters.TryGetValue("orderType", out var orderType);
            parameters.TryGetValue("transId", out var transId);
            parameters.TryGetValue("resultCode", out var resultCodeStr);
            parameters.TryGetValue("message", out var message);
            parameters.TryGetValue("responseTime", out var responseTime);
            parameters.TryGetValue("extraData", out var extraData);

            // Compute signature for validation
            var rawData = $"accessKey={_settings.AccessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderIdStr}&orderInfo={orderInfo}&partnerCode={partnerCode}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCodeStr}&transId={transId}";
            var computedSignature = HmacSha256(_settings.SecretKey, rawData);

            if (computedSignature != signature)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(orderIdStr))
            {
                var parts = orderIdStr.Split('_');
                if (parts.Length > 0 && int.TryParse(parts[0], out var parsedOrderCode))
                {
                    orderCode = parsedOrderCode;
                }
            }

            if (int.TryParse(resultCodeStr, out var resultCode) && resultCode == 0)
            {
                isSuccess = true;
            }

            return true;
        }

        private static string HmacSha256(string key, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private sealed class MomoCreateResponse
        {
            public string? PartnerCode { get; set; }
            public string? OrderId { get; set; }
            public string? RequestId { get; set; }
            public string? PayUrl { get; set; }
            public int ResultCode { get; set; }
            public string? Message { get; set; }
        }
    }
}

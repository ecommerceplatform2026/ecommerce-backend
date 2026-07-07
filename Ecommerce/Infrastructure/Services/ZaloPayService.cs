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
    public sealed class ZaloPayService : IZaloPayService
    {
        private readonly HttpClient _httpClient;
        private readonly ZaloPaySettings _settings;

        public ZaloPayService(HttpClient httpClient, IOptions<ZaloPaySettings> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<string> CreatePaymentUrlAsync(int orderCode, long totalAmount, CancellationToken cancellationToken = default)
        {
            var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var appTransId = DateTime.UtcNow.ToString("yyMMdd") + "_" + orderCode.ToString() + "_" + Guid.NewGuid().ToString("N").Substring(0, 4);
            var appUser = "demo";
            var embedData = "{}";
            var item = "[]";
            var description = $"Thanh toan don hang {orderCode}";

            // mac: appid | apptransid | appuser | amount | apptime | embeddata | item
            var rawData = $"{_settings.AppId}|{appTransId}|{appUser}|{totalAmount}|{appTime}|{embedData}|{item}";
            var mac = HmacSha256(_settings.Key1, rawData);

            var requestBody = new
            {
                appid = _settings.AppId,
                apptransid = appTransId,
                appuser = appUser,
                apptime = appTime,
                amount = totalAmount,
                item,
                embeddata = embedData,
                description,
                bankcode = "zalopayapp",
                callback_url = _settings.NotifyUrl,
                redirect_url = _settings.ReturnUrl,
                mac
            };

            var response = await _httpClient.PostAsJsonAsync(_settings.CreateUrl, requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"ZaloPay API returned {(int)response.StatusCode}: {errorBody}");
            }

            var resData = await response.Content.ReadFromJsonAsync<ZaloPayCreateResponse>(cancellationToken: cancellationToken);

            if (resData == null || resData.ReturnCode != 1)
            {
                var errorMsg = resData == null
                    ? "null response"
                    : $"code={resData.ReturnCode} sub={resData.SubReturnCode}: {resData.ReturnMessage ?? "no message"}";
                throw new InvalidOperationException($"ZaloPay payment creation failed: {errorMsg}");
            }

            return resData.OrderUrl!;
        }

        public bool ValidateCallback(IDictionary<string, string> parameters, out int orderCode, out bool isSuccess)
        {
            orderCode = 0;
            isSuccess = false;

            if (parameters == null ||
                !parameters.TryGetValue("data", out var data) ||
                !parameters.TryGetValue("mac", out var mac))
            {
                return false;
            }

            var computedMac = HmacSha256(_settings.Key2, data);
            if (computedMac != mac)
                return false;

            using var doc = JsonDocument.Parse(data);
            if (!doc.RootElement.TryGetProperty("app_trans_id", out var appTransIdEl))
                return false;

            var appTransId = appTransIdEl.GetString();
            if (string.IsNullOrEmpty(appTransId))
                return false;

            var parts = appTransId.Split('_');
            if (parts.Length <= 1 || !int.TryParse(parts[1], out var parsedOrderCode))
                return false;

            orderCode = parsedOrderCode;
            isSuccess = true;
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

        private sealed class ZaloPayCreateResponse
        {
            public int ReturnCode { get; set; }
            public string? ReturnMessage { get; set; }
            public int SubReturnCode { get; set; }
            public string? SubReturnMessage { get; set; }
            public string? OrderUrl { get; set; }
            public string? OrderToken { get; set; }
        }
    }
}

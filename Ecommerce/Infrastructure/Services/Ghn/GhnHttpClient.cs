using Microsoft.Extensions.Options;
using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services.Ghn
{
    /// <summary>
    /// Low-level GHN HTTP client. Internal to GHN provider.
    /// Handles auth headers, snake_case serialization, error parsing.
    /// </summary>
    public sealed class GhnHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly GhnOptions _options;

        public GhnHttpClient(HttpClient httpClient, IOptions<GhnOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("Token", _options.Token);
            _httpClient.DefaultRequestHeaders.Add("ShopId", _options.ShopId.ToString());
        }

        public async Task<GhnApiResponse<T>> PostAsync<T>(
            string endpoint,
            object payload,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                var result = JsonSerializer.Deserialize<GhnApiResponse<T>>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new GhnApiResponse<T>
                {
                    Code = (int)response.StatusCode,
                    Message = "Failed to parse GHN response."
                };
            }
            catch (HttpRequestException ex)
            {
                return new GhnApiResponse<T>
                {
                    Code = 500,
                    Message = $"GHN API request failed: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                return new GhnApiResponse<T>
                {
                    Code = 408,
                    Message = "GHN API request timed out."
                };
            }
        }
    }
}

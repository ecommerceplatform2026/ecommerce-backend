using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Configurations;
using Application.DTOs.Product;
using Application.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services
{
    public sealed class CloudinaryProductImageStorage : IProductImageStorage
    {
        private readonly HttpClient _httpClient;
        private readonly CloudinarySettings _settings;

        public CloudinaryProductImageStorage(HttpClient httpClient, IOptions<CloudinarySettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<ProductImageUploadResult> UploadAsync(
            Stream imageStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var signature = CreateSignature(new Dictionary<string, string>
            {
                ["folder"] = _settings.Folder,
                ["timestamp"] = timestamp
            });

            using var content = new MultipartFormDataContent
            {
                { new StringContent(_settings.ApiKey), "api_key" },
                { new StringContent(timestamp), "timestamp" },
                { new StringContent(_settings.Folder), "folder" },
                { new StringContent(signature), "signature" }
            };

            var fileContent = new StreamContent(imageStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            using var response = await _httpClient.PostAsync(
                $"https://api.cloudinary.com/v1_1/{_settings.CloudName}/image/upload",
                content,
                cancellationToken);

            var uploadResponse = await ReadCloudinaryResponseAsync<CloudinaryUploadResponse>(
                response.Content,
                "Cloudinary image upload failed.",
                cancellationToken);

            if (!response.IsSuccessStatusCode || uploadResponse?.SecureUrl is null || uploadResponse.PublicId is null)
            {
                var message = uploadResponse?.Error?.Message ?? "Cloudinary image upload failed.";
                throw new InvalidOperationException(message);
            }

            return new ProductImageUploadResult
            {
                ImageUrl = uploadResponse.SecureUrl,
                PublicId = uploadResponse.PublicId
            };
        }

        public async Task DeleteAsync(string publicId, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            if (string.IsNullOrWhiteSpace(publicId))
                throw new InvalidOperationException("Cloudinary public id is required.");

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var signature = CreateSignature(new Dictionary<string, string>
            {
                ["public_id"] = publicId,
                ["timestamp"] = timestamp
            });

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["api_key"] = _settings.ApiKey,
                ["timestamp"] = timestamp,
                ["public_id"] = publicId,
                ["signature"] = signature
            });

            using var response = await _httpClient.PostAsync(
                $"https://api.cloudinary.com/v1_1/{_settings.CloudName}/image/destroy",
                content,
                cancellationToken);

            var deleteResponse = await ReadCloudinaryResponseAsync<CloudinaryDeleteResponse>(
                response.Content,
                "Cloudinary image delete failed.",
                cancellationToken);

            if (!response.IsSuccessStatusCode || string.Equals(deleteResponse?.Result, "error", StringComparison.OrdinalIgnoreCase))
            {
                var message = deleteResponse?.Error?.Message ?? "Cloudinary image delete failed.";
                throw new InvalidOperationException(message);
            }
        }

        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_settings.CloudName) ||
                string.IsNullOrWhiteSpace(_settings.ApiKey) ||
                string.IsNullOrWhiteSpace(_settings.ApiSecret))
            {
                throw new InvalidOperationException("Cloudinary settings are not configured.");
            }
        }

        private static async Task<T?> ReadCloudinaryResponseAsync<T>(
            HttpContent content,
            string failureMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                return await content.ReadFromJsonAsync<T>(cancellationToken);
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                throw new InvalidOperationException(failureMessage, ex);
            }
        }

        private string CreateSignature(IReadOnlyDictionary<string, string> parameters)
        {
            var payload = string.Join(
                "&",
                parameters
                    .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Value))
                    .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                    .Select(parameter => $"{parameter.Key}={parameter.Value}"));

            var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(payload + _settings.ApiSecret));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private sealed class CloudinaryUploadResponse
        {
            [JsonPropertyName("secure_url")]
            public string? SecureUrl { get; set; }

            [JsonPropertyName("public_id")]
            public string? PublicId { get; set; }

            [JsonPropertyName("error")]
            public CloudinaryError? Error { get; set; }
        }

        private sealed class CloudinaryDeleteResponse
        {
            [JsonPropertyName("result")]
            public string? Result { get; set; }

            [JsonPropertyName("error")]
            public CloudinaryError? Error { get; set; }
        }

        private sealed class CloudinaryError
        {
            [JsonPropertyName("message")]
            public string? Message { get; set; }
        }
    }
}

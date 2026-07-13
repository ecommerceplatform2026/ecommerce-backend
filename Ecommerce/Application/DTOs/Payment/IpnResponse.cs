using System.Text.Json.Serialization;

namespace Application.DTOs.Payment
{
    public sealed record VnPayIpnResponse
    {
        [JsonPropertyName("RspCode")]
        public string RspCode { get; init; } = string.Empty;

        [JsonPropertyName("Message")]
        public string Message { get; init; } = string.Empty;
    }

    public sealed record MomoIpnResponse
    {
        [JsonPropertyName("resultCode")]
        public int ResultCode { get; init; }

        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;
    }

    public sealed record ZaloPayIpnResponse
    {
        [JsonPropertyName("return_code")]
        public int ReturnCode { get; init; }

        [JsonPropertyName("return_message")]
        public string ReturnMessage { get; init; } = string.Empty;
    }
}

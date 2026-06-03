using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace Ecommerce.UnitTests.Services
{
    public class GhnSandboxConnectionTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly string _baseUrl;
        private readonly string _token;
        private readonly int _shopId;

        public GhnSandboxConnectionTests()
        {
            var json = File.ReadAllText("appsettings.Testing.json");
            using var doc = JsonDocument.Parse(json);
            var ghn = doc.RootElement.GetProperty("GHN");
            _token = ghn.GetProperty("Token").GetString()!;
            _shopId = ghn.GetProperty("ShopId").GetInt32();
            _baseUrl = ghn.GetProperty("BaseUrl").GetString()!;
        }

        [Fact]
        public async Task Sandbox_WithoutAuth_Returns401()
        {
            using var client = new HttpClient { BaseAddress = new Uri(_baseUrl) };

            var response = await client.PostAsync(
                "master-data/province",
                new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Sandbox_WithInvalidToken_Returns401()
        {
            using var client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            client.DefaultRequestHeaders.Add("Token", "invalid-token");
            client.DefaultRequestHeaders.Add("ShopId", _shopId.ToString());

            var response = await client.PostAsync(
                "master-data/province",
                new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Sandbox_WithValidToken_DoesNotReturn401()
        {
            using var client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            client.DefaultRequestHeaders.Add("Token", _token);
            client.DefaultRequestHeaders.Add("ShopId", _shopId.ToString());

            var response = await client.PostAsync(
                "master-data/province",
                new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

            response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Sandbox_GetProvinces_Returns200WithData()
        {
            using var client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            client.DefaultRequestHeaders.Add("Token", _token);
            client.DefaultRequestHeaders.Add("ShopId", _shopId.ToString());

            var response = await client.PostAsync(
                "master-data/province",
                new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GhnApiResponse<GhnProvince[]>>(body, JsonOptions);

            result.Should().NotBeNull();
            result!.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
            result.Data.Should().Contain(p => p.ProvinceName.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase));
        }
    }

    public class GhnApiResponse<T>
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        [JsonIgnore]
        public bool IsSuccess => Code == 200;
    }

    public class GhnProvince
    {
        public int ProvinceID { get; set; }
        public string ProvinceName { get; set; } = string.Empty;
    }
}

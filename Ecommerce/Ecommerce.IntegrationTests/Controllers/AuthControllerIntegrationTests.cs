using Application.DTOs.Auth;
using Ecommerce.IntegrationTests.Helpers;
using FluentAssertions;
using Presentation.Common.Responses;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.IntegrationTests.Controllers
{
    public class AuthControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AuthControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task RegisterAndLogin_SuccessfulFlow()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                FullName = "Integration User",
                Email = "integration_test@example.com",
                Password = "Password123"
            };

            // Act - Register
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
            
            // Assert
            registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
            registerResult.Should().NotBeNull();
            registerResult!.Success.Should().BeTrue();
            registerResult.Data.Should().NotBeNull();
            registerResult.Data!.Email.Should().Be("integration_test@example.com");
            registerResult.Data.Token.Should().NotBeNullOrEmpty();

            // Arrange - Login
            var loginRequest = new LoginRequest
            {
                Email = "integration_test@example.com",
                Password = "Password123"
            };

            // Act - Login
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
            loginResult.Should().NotBeNull();
            loginResult!.Success.Should().BeTrue();
            loginResult.Data.Should().NotBeNull();
            loginResult.Data!.Email.Should().Be("integration_test@example.com");
            loginResult.Data.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ReturnsConflict()
        {
            // Arrange
            var email = "duplicate@example.com";
            var registerRequest1 = new RegisterRequest
            {
                FullName = "User One",
                Email = email,
                Password = "Password123"
            };

            // Register first user
            await _client.PostAsJsonAsync("/api/auth/register", registerRequest1);

            // Register second user with same email
            var registerRequest2 = new RegisterRequest
            {
                FullName = "User Two",
                Email = email,
                Password = "Password123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest2);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Errors.Should().Contain("Email is already registered.");
        }
    }
}

using Application.DTOs.User;
using Domain.Entities;
using Ecommerce.IntegrationTests.Helpers;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Common.Responses;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.IntegrationTests.Controllers
{
    public class UserControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public UserControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private string GetTokenForUser(User user)
        {
            using var scope = _factory.Services.CreateScope();
            var jwtService = scope.ServiceProvider.GetRequiredService<Application.Interfaces.Services.IJwtService>();
            return jwtService.GenerateToken(user);
        }

        private void AuthenticateClient(HttpClient client, User user)
        {
            var token = GetTokenForUser(user);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        [Fact]
        public async Task GetUser_ReturnsOk_WithUserProfile()
        {
            // Arrange
            var user = User.Create("Profile User", "profile@example.com", "hash");
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
            AuthenticateClient(_client, user);

            // Act
            var response = await _client.GetAsync("/api/profile");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.FullName.Should().Be("Profile User");
        }

        [Fact]
        public async Task UpdateUser_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var user = User.Create("Profile User 2", "profile2@example.com", "hash");
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
            AuthenticateClient(_client, user);

            var request = new UpdateUserRequest
            {
                FullName = "Updated Profile Name",
                PhoneNumber = "0987654321",
                DateOfBirth = new DateTime(1995, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/profile", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.FullName.Should().Be("Updated Profile Name");
            result.Data.PhoneNumber.Should().Be("0987654321");
        }
    }
}

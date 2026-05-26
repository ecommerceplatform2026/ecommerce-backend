using Application.DTOs.Dashboard;
using Domain.Entities;
using Domain.Enums;
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
    public class AdminDashboardControllerIntegrationTests : IClassFixture<AdminDashboardWebFactory>
    {
        private readonly AdminDashboardWebFactory _factory;
        private readonly HttpClient _client;

        public AdminDashboardControllerIntegrationTests(AdminDashboardWebFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private void AuthenticateClient(HttpClient client, User user)
        {
            using var scope = _factory.Services.CreateScope();
            var jwtService = scope.ServiceProvider.GetRequiredService<Application.Interfaces.Services.IJwtService>();
            var token = jwtService.GenerateToken(user);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        [Fact]
        public async Task GetSummary_ReturnsForbidden_WhenUserIsNotAdmin()
        {
            // Arrange
            var regularUser = User.Create("Regular User", "user_dash@example.com", "hash");
            regularUser.Role = UserRole.User;
            AuthenticateClient(_client, regularUser);

            // Act
            var response = await _client.GetAsync("/api/admin/dashboard/summary");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetSummary_ReturnsOk_WhenUserIsAdmin()
        {
            // Arrange: admin user (doesn't need to be in DB — token carries the role)
            var adminUser = User.Create("Admin User", "admin_dash@example.com", "hash");
            adminUser.Role = UserRole.Admin;
            AuthenticateClient(_client, adminUser);

            // Act: call without date params so controller doesn't validate dates and
            // dashboard repo just returns empty aggregates from an empty InMemory store
            var response = await _client.GetAsync("/api/admin/dashboard/summary");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<DashboardSummaryResponse>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }
    }

    /// <summary>Dedicated factory with isolated DB for AdminDashboard tests.</summary>
    public class AdminDashboardWebFactory : CustomWebApplicationFactory
    {
        public AdminDashboardWebFactory() : base($"AdminDashboardDb_{Guid.NewGuid():N}") { }
    }
}

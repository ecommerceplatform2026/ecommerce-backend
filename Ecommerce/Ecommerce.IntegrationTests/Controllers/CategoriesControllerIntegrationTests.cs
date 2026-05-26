using Application.DTOs.Category;
using Domain.Entities;
using Domain.Enums;
using Ecommerce.IntegrationTests.Helpers;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Common.Responses;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.IntegrationTests.Controllers
{
    public class CategoriesControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CategoriesControllerIntegrationTests(CustomWebApplicationFactory factory)
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
        public async Task GetCategories_ReturnsAllActiveCategories()
        {
            // Arrange
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
                db.Categories.Add(Category.Create("Category Integrations"));
                await db.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync("/api/categories");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryResponse>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().Contain(c => c.Name == "Category Integrations");
        }

        [Fact]
        public async Task CreateCategory_ReturnsForbidden_WhenUserIsNotAdmin()
        {
            // Arrange
            var regularUser = User.Create("Regular User", "user_cat@example.com", "hash");
            regularUser.Role = UserRole.User;
            AuthenticateClient(_client, regularUser);

            var request = new CreateCategoryRequest { Name = "Forbidden Category" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/categories", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateCategory_ReturnsOk_WhenUserIsAdmin()
        {
            // Arrange
            var adminUser = User.Create("Admin User", "admin_cat@example.com", "hash");
            adminUser.Role = UserRole.Admin;
            AuthenticateClient(_client, adminUser);

            var request = new CreateCategoryRequest { Name = "Admin Category Creation" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/categories", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.Name.Should().Be("Admin Category Creation");
        }
    }
}

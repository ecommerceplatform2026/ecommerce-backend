using Application.DTOs.Product;
using Domain.Entities;
using Domain.Common;
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
    public class ProductsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ProductsControllerIntegrationTests(CustomWebApplicationFactory factory)
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
        public async Task GetProducts_ReturnsAllProducts()
        {
            // Arrange
            Guid categoryId;
            Guid productId = Guid.NewGuid();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
                
                var category = Category.Create("Test Category");
                db.Categories.Add(category);
                await db.SaveChangesAsync();
                categoryId = category.Id;

                var product = Product.Create(categoryId, "Integration Product", "Description", "Cotton", new Money(200000), ProductStatus.Active);
                // Set the ID manually for deterministic lookup if needed, or let DB generate
                db.Products.Add(product);
                await db.SaveChangesAsync();
                productId = product.Id;
            }

            // Act
            var response = await _client.GetAsync("/api/products");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProductResponse>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().Contain(p => p.Name == "Integration Product");
        }

        [Fact]
        public async Task CreateProduct_ReturnsForbidden_WhenUserIsNotAdmin()
        {
            // Arrange
            var regularUser = User.Create("Regular User", "user@example.com", "hash");
            regularUser.Role = UserRole.User;
            AuthenticateClient(_client, regularUser);

            var request = new CreateProductRequest
            {
                Name = "Forbidden Product",
                Description = "Should fail",
                BasePrice = 100000,
                CategoryId = Guid.NewGuid(),
                Status = ProductStatus.Active
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/products", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateProduct_ReturnsOk_WhenUserIsAdmin()
        {
            // Arrange
            Guid categoryId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
                var category = Category.Create("Admin Category");
                db.Categories.Add(category);
                await db.SaveChangesAsync();
                categoryId = category.Id;
            }

            var adminUser = User.Create("Admin User", "admin@example.com", "hash");
            adminUser.Role = UserRole.Admin;
            AuthenticateClient(_client, adminUser);

            var request = new CreateProductRequest
            {
                Name = "Admin Product",
                Description = "Created by admin",
                BasePrice = 150000,
                CategoryId = categoryId,
                Status = ProductStatus.Active
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/products", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be("Admin Product");
        }
    }
}

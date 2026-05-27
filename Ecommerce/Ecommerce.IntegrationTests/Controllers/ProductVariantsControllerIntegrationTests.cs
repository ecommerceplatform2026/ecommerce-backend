using Application.DTOs.Product.ProductVariants;
using Domain.Entities;
using Domain.Common;
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
    public class ProductVariantsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ProductVariantsControllerIntegrationTests(CustomWebApplicationFactory factory)
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
        public async Task GetVariants_ReturnsOk()
        {
            // Arrange
            Guid categoryId;
            Guid productId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
                
                var category = Category.Create("Test Cat");
                db.Categories.Add(category);
                await db.SaveChangesAsync();
                categoryId = category.Id;

                var product = Product.Create(categoryId, "Test Product for Variant", "Desc", "Material", new Money(100000), ProductStatus.Active);
                product.AddVariant("SKUVAR1", "Red", "M", 100, new Money(100000), 5);
                db.Products.Add(product);
                await db.SaveChangesAsync();
                productId = product.Id;
            }

            // Act
            var response = await _client.GetAsync($"/api/products/{productId}/variants");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProductVariantResponse>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().Contain(v => v.SKU == "SKUVAR1");
        }

        [Fact]
        public async Task AddVariant_ReturnsForbidden_WhenUserIsNotAdmin()
        {
            // Arrange
            var regularUser = User.Create("Regular User", "user_var@example.com", "hash");
            regularUser.Role = UserRole.User;
            AuthenticateClient(_client, regularUser);

            var request = new CreateProductVariantRequest
            {
                SKU = "VARFORBIDDEN",
                Color = "Black",
                Size = "L",
                Stock = 10,
                Price = 50000,
                LowStockThreshold = 2
            };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/products/{Guid.NewGuid()}/variants", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}

using Application.DTOs.Cart;
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
    public class CartControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CartControllerIntegrationTests(CustomWebApplicationFactory factory)
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
        public async Task GetCart_ReturnsEmptyList_WhenCartIsEmpty()
        {
            // Arrange
            var user = User.Create("Cart User 1", "cart1@example.com", "hash");
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
            AuthenticateClient(_client, user);

            // Act
            var response = await _client.GetAsync("/api/cart");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CartItemResponse>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task AddToCart_ReturnsOk_WithItem()
        {
            // Arrange
            var user = User.Create("Cart User 2", "cart2@example.com", "hash");
            Guid variantId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
                db.Users.Add(user);
                
                var category = Category.Create("Cart Category");
                db.Categories.Add(category);
                await db.SaveChangesAsync();

                var product = Product.Create(category.Id, "Cart Product", "Desc", "Material", new Money(100000), ProductStatus.Active);
                product.AddVariant("CARTSKU1", "Red", "M", 100, new Money(100000), 5);
                db.Products.Add(product);
                await db.SaveChangesAsync();
                variantId = product.ProductVariants.GetEnumerator().Current?.Id ?? Guid.NewGuid();
                // Wait! Let's get the actual variant ID
                foreach (var v in product.ProductVariants)
                {
                    variantId = v.Id;
                }
            }
            AuthenticateClient(_client, user);

            var request = new AddToCartRequest(variantId, 2);

            // Act
            var response = await _client.PostAsJsonAsync("/api/cart/items", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CartItemResponse>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.ProductVariantId.Should().Be(variantId);
            result.Data!.Quantity.Should().Be(2);
        }
    }
}

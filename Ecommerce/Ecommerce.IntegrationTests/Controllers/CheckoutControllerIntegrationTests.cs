using Application.DTOs.Checkout;
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
    public class CheckoutControllerIntegrationTests : IClassFixture<CheckoutWebFactory>
    {
        private readonly CheckoutWebFactory _factory;
        private readonly HttpClient _client;

        public CheckoutControllerIntegrationTests(CheckoutWebFactory factory)
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
        public async Task Checkout_ReturnsFailure_WhenCartIsEmpty()
        {
            // Arrange: seed user only (no cart items)
            var user = User.Create("Checkout User 1", "checkout1@example.com", "hash");
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
            AuthenticateClient(_client, user);

            var request = new CheckoutRequest(PaymentMethod.COD);

            // Act
            var response = await _client.PostAsJsonAsync("/api/checkout", request);

            // Assert: empty cart → Failure → 400 BadRequest
            var content = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != HttpStatusCode.BadRequest)
            {
                throw new Exception($"Test failed. Status code: {response.StatusCode}. Response body: {content}");
            }
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutResponse>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Errors.Should().Contain("Cart is empty.");
        }

        [Fact]
        public async Task Checkout_Succeeds_WhenCartHasItems()
        {
            // Arrange
            var user = User.Create("Checkout User 2", "checkout2@example.com", "hash");
            Guid variantId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
                db.Users.Add(user);

                var category = Category.Create("Checkout Category");
                db.Categories.Add(category);
                await db.SaveChangesAsync();

                var product = Product.Create(
                    category.Id,
                    "Checkout Product",
                    "Desc",
                    "Silk",
                    new Money(100000),
                    ProductStatus.Active);

                product.AddVariant("CHKOUTSKU", "Red", "M", 50, new Money(100000), 5);
                db.Products.Add(product);
                await db.SaveChangesAsync();

                variantId = Guid.Empty;
                foreach (var v in product.ProductVariants)
                {
                    variantId = v.Id;
                }

                var cartItem = new CartItem
                {
                    UserId = user.Id,
                    ProductVariantId = variantId,
                    Quantity = 2
                };
                db.CartItems.Add(cartItem);
                await db.SaveChangesAsync();
            }
            AuthenticateClient(_client, user);

            var request = new CheckoutRequest(PaymentMethod.COD);

            // Act
            var response = await _client.PostAsJsonAsync("/api/checkout", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CheckoutResponse>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalAmount.Should().Be(200000);
            result.Data.Status.Should().Be(OrderStatus.Pending);
        }
    }

    /// <summary>Dedicated factory with isolated DB for Checkout tests.</summary>
    public class CheckoutWebFactory : CustomWebApplicationFactory
    {
        public CheckoutWebFactory() : base($"CheckoutDb_{Guid.NewGuid():N}") { }
    }
}

using Application.DTOs.Review;
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
    public class ReviewsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ReviewsControllerIntegrationTests(CustomWebApplicationFactory factory)
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
        public async Task CreateReview_ReturnsOk_WhenUserHasPurchasedProduct()
        {
            // Arrange
            var user = User.Create("Review User", "review@example.com", "hash");
            Guid categoryId;
            Guid productId;
            Guid variantId;
            Guid orderId;

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
                db.Users.Add(user);

                var category = Category.Create("Review Cat");
                db.Categories.Add(category);
                await db.SaveChangesAsync();
                categoryId = category.Id;

                var product = Product.Create(categoryId, "Review Product", "Desc", "Material", new Money(100000), ProductStatus.Active);
                product.AddVariant("REVSKU1", "Black", "S", 10, new Money(100000), 2);
                db.Products.Add(product);
                await db.SaveChangesAsync();
                productId = product.Id;
                
                variantId = Guid.Empty;
                foreach (var v in product.ProductVariants)
                {
                    variantId = v.Id;
                }

                // Add a confirmed paid order with this product variant
                var order = Order.Create(user.Id, 777666, PaymentMethod.COD);
                order.AddItem(variantId, 1, new Money(100000), "snapshot");
                order.ConfirmPayment(); // mark as confirmed/paid
                db.Orders.Add(order);
                await db.SaveChangesAsync();
                orderId = order.Id;
            }

            AuthenticateClient(_client, user);

            var request = new CreateReviewRequest(
                ProductId: productId,
                OrderId: orderId,
                Rating: 5,
                Title: "Excellent",
                Comment: "Love the material!"
            );

            // Act
            var response = await _client.PostAsJsonAsync("/api/reviews", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReviewResponse>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Rating.Should().Be(5);
        }
    }
}

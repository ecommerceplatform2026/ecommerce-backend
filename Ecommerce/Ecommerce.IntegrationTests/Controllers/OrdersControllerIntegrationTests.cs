using Application.DTOs.Order;
using Domain.Entities;
using Domain.Enums;
using Domain.Common;
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
    public class OrdersControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OrdersControllerIntegrationTests(CustomWebApplicationFactory factory)
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
        public async Task GetMyOrders_ReturnsOk_WithOrders()
        {
            // Arrange
            var user = User.Create("Order User 1", "order1@example.com", "hash");
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
                db.Users.Add(user);
                await db.SaveChangesAsync();

                var order = Order.Create(user.Id, 112233, PaymentMethod.COD);
                db.Orders.Add(order);
                await db.SaveChangesAsync();
            }
            AuthenticateClient(_client, user);

            // Act
            var response = await _client.GetAsync("/api/orders?page=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<Application.Common.Response.PagedResult<OrderResponse>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().Contain(o => o.OrderCode == 112233);
        }
    }
}

using Application.DTOs.Payment;
using Domain.Entities;
using Domain.Common;
using Domain.Enums;
using Ecommerce.IntegrationTests.Helpers;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Presentation.Common.Responses;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.IntegrationTests.Controllers
{
    public class PaymentsControllerIntegrationTests : IClassFixture<PaymentsWebFactory>
    {
        private readonly PaymentsWebFactory _factory;
        private readonly HttpClient _client;

        public PaymentsControllerIntegrationTests(PaymentsWebFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task VnPayReturn_ProcessesCallback_AndUpdatesStatus()
        {
            // Arrange: setup VnPay mock to return success for our order code
            var orderCode = 998877;

            // Moq out/ref parameters: use Callback or the out-param overload
            _factory.VnPayServiceMock
                .Setup(v => v.ValidateCallback(
                    It.IsAny<IDictionary<string, string>>(),
                    out It.Ref<int>.IsAny,
                    out It.Ref<bool>.IsAny))
                .Returns((IDictionary<string, string> _, ref int code, ref bool success) =>
                {
                    code = orderCode;
                    success = true;
                    return true;
                });

            // Seed: user → order → payment (all in one scope for clean InMemory tracking)
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();

                var user = User.Create("Payment User", "payment@example.com", "hash");
                db.Users.Add(user);
                await db.SaveChangesAsync();

                var order = Order.Create(user.Id, orderCode, PaymentMethod.VNPay);
                db.Orders.Add(order);
                await db.SaveChangesAsync();

                var payment = Payment.Create(order.Id, orderCode, new Money(50000), "link_id", "checkout_url");
                db.Payments.Add(payment);
                await db.SaveChangesAsync();
            }

            // Act
            var response = await _client.GetAsync($"/api/payments/vnpay/callback?vnp_TxnRef={orderCode}&vnp_ResponseCode=00");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("00");
            body.Should().Contain("success");
        }
    }

    /// <summary>Dedicated factory with isolated DB for Payments tests.</summary>
    public class PaymentsWebFactory : CustomWebApplicationFactory
    {
        public PaymentsWebFactory() : base($"PaymentsDb_{Guid.NewGuid():N}") { }
    }
}

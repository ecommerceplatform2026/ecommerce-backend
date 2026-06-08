using Application.Interfaces.Services;
using Application.Events;
using Domain.Entities;
using Domain.Events;
using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Enums;

namespace Ecommerce.UnitTests.EventTests
{
    public class AwardLoyaltyPointsOnOrderDeliveredHandlerTests
    {
        private readonly Mock<ILoyaltyService> _loyaltyServiceMock;
        private readonly AwardLoyaltyPointsOnOrderDeliveredHandler _handler;

        public AwardLoyaltyPointsOnOrderDeliveredHandlerTests()
        {
            _loyaltyServiceMock = new Mock<ILoyaltyService>();
            _handler = new AwardLoyaltyPointsOnOrderDeliveredHandler(_loyaltyServiceMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenOrderDeliveredEventReceived_CallsLoyaltyServiceOnce()
        {
            var order = Order.Create(Guid.NewGuid(), 100001, PaymentMethod.COD);
            var domainEvent = new OrderDeliveredDomainEvent(order);
            var cancellationToken = new CancellationTokenSource().Token;

            _loyaltyServiceMock
                .Setup(s => s.AwardPendingPointsForDeliveredOrderAsync(order.Id, cancellationToken))
                .ReturnsAsync(global::Application.Common.Response.Result<int>.Success(2));

            await _handler.HandleAsync(domainEvent, cancellationToken);

            _loyaltyServiceMock.Verify(
                s => s.AwardPendingPointsForDeliveredOrderAsync(order.Id, cancellationToken),
                Times.Once);
        }
    }
}

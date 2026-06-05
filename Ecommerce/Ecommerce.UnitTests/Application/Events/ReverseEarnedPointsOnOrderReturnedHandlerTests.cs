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
    public class ReverseEarnedPointsOnOrderReturnedHandlerTests
    {
        private readonly Mock<ILoyaltyService> _loyaltyServiceMock;
        private readonly ReverseEarnedPointsOnOrderReturnedHandler _handler;

        public ReverseEarnedPointsOnOrderReturnedHandlerTests()
        {
            _loyaltyServiceMock = new Mock<ILoyaltyService>();
            _handler = new ReverseEarnedPointsOnOrderReturnedHandler(_loyaltyServiceMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenOrderReturnedEventReceived_CallsReverseOnce()
        {
            var order = Order.Create(Guid.NewGuid(), 100001, PaymentMethod.COD);
            var domainEvent = new OrderReturnedDomainEvent(order);
            var cancellationToken = new CancellationTokenSource().Token;

            _loyaltyServiceMock
                .Setup(s => s.ReverseEarnedPointsForReturnedOrderAsync(order.Id, cancellationToken))
                .ReturnsAsync(Application.Common.Response.Result<int>.Success(300));

            await _handler.HandleAsync(domainEvent, cancellationToken);

            _loyaltyServiceMock.Verify(
                s => s.ReverseEarnedPointsForReturnedOrderAsync(order.Id, cancellationToken),
                Times.Once);
        }

        [Fact]
        public void HandleAsync_NullEvent_ThrowsArgumentNullException()
        {
            Action act = () => _handler.HandleAsync(null!, CancellationToken.None);

            act.Should().Throw<ArgumentNullException>();
        }
    }
}

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
    public class RefundRedeemedPointsOnOrderCancelledHandlerTests
    {
        private readonly Mock<ILoyaltyService> _loyaltyServiceMock;
        private readonly RefundRedeemedPointsOnOrderCancelledHandler _handler;

        public RefundRedeemedPointsOnOrderCancelledHandlerTests()
        {
            _loyaltyServiceMock = new Mock<ILoyaltyService>();
            _handler = new RefundRedeemedPointsOnOrderCancelledHandler(_loyaltyServiceMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenOrderCancelledEventReceived_CallsRefundOnce()
        {
            var order = Order.Create(Guid.NewGuid(), 100001, PaymentMethod.COD);
            var domainEvent = new OrderCancelledDomainEvent(order);
            var cancellationToken = new CancellationTokenSource().Token;

            _loyaltyServiceMock
                .Setup(s => s.CancelPendingTransactionsForOrderAsync(order.Id, cancellationToken))
                .ReturnsAsync(Application.Common.Response.Result<int>.Success(300));

            await _handler.HandleAsync(domainEvent, cancellationToken);

            _loyaltyServiceMock.Verify(
                s => s.CancelPendingTransactionsForOrderAsync(order.Id, cancellationToken),
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

using Application.Common.Response;
using Application.Events;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.UnitTests.EventTests
{
    public class CompleteLoyaltyTransactionsOnOrderCompletedHandlerTests
    {
        private readonly Mock<ILoyaltyService> _loyaltyServiceMock;
        private readonly CompleteLoyaltyTransactionsOnOrderCompletedHandler _handler;

        public CompleteLoyaltyTransactionsOnOrderCompletedHandlerTests()
        {
            _loyaltyServiceMock = new Mock<ILoyaltyService>();
            _handler = new CompleteLoyaltyTransactionsOnOrderCompletedHandler(_loyaltyServiceMock.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenOrderCompletedEventReceived_CallsLoyaltyServiceOnce()
        {
            var order = Order.Create(Guid.NewGuid(), 100001, PaymentMethod.COD);
            var domainEvent = new OrderCompletedDomainEvent(order);
            var cancellationToken = new CancellationTokenSource().Token;

            _loyaltyServiceMock
                .Setup(s => s.CompletePendingTransactionsForOrderAsync(order.Id, cancellationToken))
                .ReturnsAsync(Result<int>.Success(1));

            await _handler.HandleAsync(domainEvent, cancellationToken);

            _loyaltyServiceMock.Verify(
                s => s.CompletePendingTransactionsForOrderAsync(order.Id, cancellationToken),
                Times.Once);
        }
    }
}

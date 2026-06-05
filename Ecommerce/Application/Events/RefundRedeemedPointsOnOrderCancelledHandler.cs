using Application.Interfaces.Events;
using Application.Interfaces.Services;
using Domain.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events
{
    public sealed class RefundRedeemedPointsOnOrderCancelledHandler : IDomainEventHandler<OrderCancelledDomainEvent>
    {
        private readonly ILoyaltyService _loyaltyService;

        public RefundRedeemedPointsOnOrderCancelledHandler(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        }

        public Task HandleAsync(OrderCancelledDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            if (domainEvent == null)
            {
                throw new ArgumentNullException(nameof(domainEvent));
            }

            return _loyaltyService.RefundRedeemedPointsForOrderAsync(domainEvent.Order.Id, cancellationToken);
        }
    }
}

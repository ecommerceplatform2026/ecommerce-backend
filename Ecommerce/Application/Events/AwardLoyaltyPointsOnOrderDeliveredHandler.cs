using Application.Interfaces.Events;
using Application.Interfaces.Services;
using Domain.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events
{
    public sealed class AwardLoyaltyPointsOnOrderDeliveredHandler : IDomainEventHandler<OrderDeliveredDomainEvent>
    {
        private readonly ILoyaltyService _loyaltyService;

        public AwardLoyaltyPointsOnOrderDeliveredHandler(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        }

        public Task HandleAsync(OrderDeliveredDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            if (domainEvent == null)
            {
                throw new ArgumentNullException(nameof(domainEvent));
            }

            return _loyaltyService.AwardPendingPointsForDeliveredOrderAsync(domainEvent.Order.Id, cancellationToken);
        }
    }
}

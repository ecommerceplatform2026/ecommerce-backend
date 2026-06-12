using Application.Interfaces.Events;
using Application.Interfaces.Services;
using Domain.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events
{
    public sealed class ReverseEarnedPointsOnOrderReturnedHandler : IDomainEventHandler<OrderReturnedDomainEvent>
    {
        private readonly ILoyaltyService _loyaltyService;

        public ReverseEarnedPointsOnOrderReturnedHandler(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        }

        public Task HandleAsync(OrderReturnedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            return _loyaltyService.CancelPendingTransactionsForOrderAsync(domainEvent.Order.Id, cancellationToken);
        }
    }
}

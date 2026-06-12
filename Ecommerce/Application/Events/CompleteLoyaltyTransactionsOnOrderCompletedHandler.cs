using Application.Interfaces.Events;
using Application.Interfaces.Services;
using Domain.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events
{
    public sealed class CompleteLoyaltyTransactionsOnOrderCompletedHandler : IDomainEventHandler<OrderCompletedDomainEvent>
    {
        private readonly ILoyaltyService _loyaltyService;

        public CompleteLoyaltyTransactionsOnOrderCompletedHandler(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        }

        public async Task HandleAsync(OrderCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            await _loyaltyService.CompletePendingTransactionsForOrderAsync(domainEvent.Order.Id, cancellationToken);
        }
    }
}

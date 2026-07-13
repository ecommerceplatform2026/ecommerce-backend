using Application.Interfaces.Events;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Events;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events
{
    public sealed class ProcessLoyaltyOnOrderConfirmedHandler : IDomainEventHandler<OrderConfirmedDomainEvent>
    {
        private readonly ILoyaltyService _loyaltyService;

        public ProcessLoyaltyOnOrderConfirmedHandler(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        }

        public async Task HandleAsync(OrderConfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            var order = domainEvent.Order;
            int? redeemedPoints = order.DiscountAmount.Amount > 0
                ? (int)(order.DiscountAmount.Amount / ILoyaltyService.PointRedeemRate)
                : null;

            await _loyaltyService.CreatePendingLoyaltyTransactionsAsync(order.Id, redeemedPoints, cancellationToken);
        }
    }
}

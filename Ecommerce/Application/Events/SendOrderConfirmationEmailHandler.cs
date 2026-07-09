using Application.Interfaces.Events;
using Application.Interfaces.Services;
using Domain.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events
{
    public class SendOrderConfirmationEmailHandler : IDomainEventHandler<OrderConfirmedDomainEvent>
    {
        private readonly INotificationService _notificationService;

        public SendOrderConfirmationEmailHandler(INotificationService notificationService)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async Task HandleAsync(OrderConfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await _notificationService.SendOrderConfirmationAsync(domainEvent.Order);
        }
    }
}

using Application.Interfaces.Events;
using Application.Interfaces.Services;
using Domain.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events
{
    public class SendOrderConfirmationEmailHandler : 
        IDomainEventHandler<OrderCreatedDomainEvent>,
        IDomainEventHandler<PaymentConfirmedDomainEvent>
    {
        private readonly INotificationService _notificationService;

        public SendOrderConfirmationEmailHandler(INotificationService notificationService)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async Task HandleAsync(OrderCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await _notificationService.SendOrderConfirmationAsync(domainEvent.Order);
        }

        public async Task HandleAsync(PaymentConfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await _notificationService.SendOrderConfirmationAsync(domainEvent.Order);
        }
    }
}

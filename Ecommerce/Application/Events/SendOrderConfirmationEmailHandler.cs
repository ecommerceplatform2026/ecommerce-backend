using Application.Interfaces.Events;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events
{
    public class SendOrderConfirmationEmailHandler : 
        IDomainEventHandler<OrderConfirmedDomainEvent>,
        IDomainEventHandler<PaymentConfirmedDomainEvent>
    {
        private readonly INotificationService _notificationService;

        public SendOrderConfirmationEmailHandler(INotificationService notificationService)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async Task HandleAsync(OrderConfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            if (domainEvent.Order.PaymentMethod == PaymentMethod.COD)
            {
                await _notificationService.SendOrderConfirmationAsync(domainEvent.Order);
            }
        }

        public async Task HandleAsync(PaymentConfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await _notificationService.SendOrderConfirmationAsync(domainEvent.Order);
        }
    }
}

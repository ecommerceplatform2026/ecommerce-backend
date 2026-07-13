using Application.Configurations;
using Application.DTOs.Delivery;
using Application.Interfaces.Events;
using Application.Interfaces.Services;
using Domain.Events;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events
{
    public class CreateDeliveryOnOrderConfirmedHandler
        : IIntegrationEventHandler<OrderConfirmedDomainEvent>
    {
        private readonly IShippingService _shippingService;
        private readonly ShippingSettings _settings;

        public CreateDeliveryOnOrderConfirmedHandler(
            IShippingService shippingService,
            IOptions<ShippingSettings> settings)
        {
            _shippingService = shippingService ?? throw new ArgumentNullException(nameof(shippingService));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task HandleAsync(OrderConfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            var result = await _shippingService.CreateShipmentAsync(
                new CreateShipmentRequest(domainEvent.Order.Id, _settings.DefaultCarrier),
                cancellationToken);

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Auto-create delivery failed for order {domainEvent.Order.Id}: {string.Join("; ", result.Errors)}");
            }
        }
    }
}

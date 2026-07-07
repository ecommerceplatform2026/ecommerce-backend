using Application.Configurations;
using Application.Interfaces.Events;
using Application.Interfaces.Services;
using Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events
{
    public class CreateDeliveryOnOrderConfirmedHandler
        : IDomainEventHandler<OrderConfirmedDomainEvent>
    {
        private readonly IShippingService _shippingService;
        private readonly ShippingSettings _settings;
        private readonly ILogger<CreateDeliveryOnOrderConfirmedHandler> _logger;

        public CreateDeliveryOnOrderConfirmedHandler(
            IShippingService shippingService,
            IOptions<ShippingSettings> settings,
            ILogger<CreateDeliveryOnOrderConfirmedHandler> logger)
        {
            _shippingService = shippingService ?? throw new ArgumentNullException(nameof(shippingService));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(OrderConfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _shippingService.CreateShipmentAsync(
                    domainEvent.Order.Id,
                    _settings.DefaultCarrier,
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Auto-create delivery failed for order {OrderId}: {Errors}",
                        domainEvent.Order.Id, string.Join("; ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-create delivery threw for order {OrderId}", domainEvent.Order.Id);
            }
        }
    }
}

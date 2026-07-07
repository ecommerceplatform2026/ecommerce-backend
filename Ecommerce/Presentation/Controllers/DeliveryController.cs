using Application.DTOs.Delivery;
using Application.DTOs.Delivery.GHN;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [Route("api/delivery")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class DeliveryController : ControllerBase
    {
        private readonly IShippingService _shippingService;
        private readonly IEnumerable<IShippingWebhookHandler> _handlers;

        public DeliveryController(
            IShippingService shippingService,
            IEnumerable<IShippingWebhookHandler> handlers)
        {
            _shippingService = shippingService ?? throw new ArgumentNullException(nameof(shippingService));
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        }

        [HttpPost("{orderId:guid}")]
        public async Task<IActionResult> CreateShipment(
            Guid orderId,
            [FromQuery] string carrier = "GHN",
            CancellationToken cancellationToken = default)
        {
            var result = await _shippingService.CreateShipmentAsync(orderId, carrier, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPost("retry")]
        public async Task<IActionResult> RetryShipment(
            [FromBody] RetryShipmentRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await _shippingService.RetryShipmentAsync(request.DeliveryId, cancellationToken);
            return this.FromResult(result);
        }

        [AllowAnonymous]
        [HttpPost("webhook/status")]
        public async Task<IActionResult> HandleDeliveryStatus(
            [FromQuery] string carrier,
            [FromBody] GhnWebhookPayload payload)
        {
            var handler = _handlers.FirstOrDefault(h =>
                h.CarrierCode.Equals(carrier, StringComparison.OrdinalIgnoreCase));

            if (handler == null)
                return Ok(new { success = false, message = $"Unknown carrier '{carrier}'." });

            var result = await handler.ProcessStatusUpdateAsync(payload, CancellationToken.None);

            return Content(result.Value ?? "{}", "application/json");
        }
    }
}

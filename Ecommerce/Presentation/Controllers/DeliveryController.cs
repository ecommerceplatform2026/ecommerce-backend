using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [Route("api/delivery")]
    [ApiController]
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

        [AllowAnonymous]
        [HttpPost("webhook/status")]
        public async Task<IActionResult> HandleDeliveryStatus(
            [FromQuery] string carrier,
            [FromBody] JsonElement payload)
        {
            var handler = _handlers.FirstOrDefault(h =>
                h.CarrierCode.Equals(carrier, StringComparison.OrdinalIgnoreCase));

            if (handler == null)
                return Ok(new { success = false, message = $"Unknown carrier '{carrier}'." });

            var result = await handler.ProcessStatusUpdateAsync(payload.GetRawText(), CancellationToken.None);

            if (!result.IsSuccess)
                return Ok(new { success = false, message = string.Join("; ", result.Errors) });

            return Ok(new { success = true });
        }
    }
}

using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public sealed class WebhooksController : ControllerBase
    {
        private readonly IEnumerable<IShippingWebhookHandler> _handlers;

        public WebhooksController(IEnumerable<IShippingWebhookHandler> handlers)
        {
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        }

        /// <summary>
        /// Receives delivery status update webhooks.
        /// Carrier determined by query param (?carrier=GHN).
        /// Always returns 200 to acknowledge receipt — carrier retries on non-200.
        /// </summary>
        [HttpPost("delivery/status")]
        public async Task<IActionResult> HandleDeliveryStatus([FromQuery] string carrier, [FromBody] JsonElement payload)
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

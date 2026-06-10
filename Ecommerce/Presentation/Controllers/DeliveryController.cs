using Application.Interfaces.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [Route("api/admin/orders")]
    [ApiController]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public class DeliveryController : ControllerBase
    {
        private readonly IShippingService _shippingService;

        public DeliveryController(IShippingService shippingService)
        {
            _shippingService = shippingService ?? throw new ArgumentNullException(nameof(shippingService));
        }

        /// <summary>
        /// Create shipment for an order via specified carrier.
        /// Admin trigger only. Order must be Pending or Confirmed.
        /// </summary>
        [HttpPut("{orderId:guid}/ship")]
        public async Task<IActionResult> CreateShipment(
            Guid orderId,
            [FromQuery] string carrier = "GHN",
            CancellationToken cancellationToken = default)
        {
            var result = await _shippingService.CreateShipmentAsync(orderId, carrier, cancellationToken);
            return this.FromResult(result);
        }
    }
}

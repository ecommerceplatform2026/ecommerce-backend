using Application.DTOs.Checkout;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Presentation.Common.Extensions;

namespace Presentation.Controllers
{
    [ApiController]
    [Authorize]
    [EnableRateLimiting("checkout-limiter")]
    [Route("api/checkout")]
    public sealed class CheckoutController : ControllerBase
    {
        private readonly ICheckoutService _checkoutService;

        public CheckoutController(ICheckoutService checkoutService)
        {
            _checkoutService = checkoutService ?? throw new ArgumentNullException(nameof(checkoutService));
        }

        [HttpPost]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken cancellationToken)
        {
            var result = await _checkoutService.ProcessCheckoutAsync(request, cancellationToken);
            return this.FromResult(result);
        }
    }
}

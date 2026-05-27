using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public sealed class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        }

        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnPayReturn(CancellationToken cancellationToken)
        {
            var queryParams = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
            var result = await _paymentService.ProcessVnPayCallbackAsync(queryParams, cancellationToken);
            return this.FromResult(result);
        }
    }
}

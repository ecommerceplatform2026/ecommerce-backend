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

        [HttpPost("momo/callback")]
        public async Task<IActionResult> MomoCallback([FromBody] Dictionary<string, string> requestBody, CancellationToken cancellationToken)
        {
            var result = await _paymentService.ProcessMomoCallbackAsync(requestBody, cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet("momo/callback")]
        public async Task<IActionResult> MomoCallbackGet(CancellationToken cancellationToken)
        {
            var queryParams = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
            var result = await _paymentService.ProcessMomoCallbackAsync(queryParams, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPost("zalopay/callback")]
        public async Task<IActionResult> ZaloPayCallback([FromBody] Dictionary<string, string> requestBody, CancellationToken cancellationToken)
        {
            var result = await _paymentService.ProcessZaloPayCallbackAsync(requestBody, cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet("zalopay/callback")]
        public async Task<IActionResult> ZaloPayCallbackGet(CancellationToken cancellationToken)
        {
            var queryParams = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
            var result = await _paymentService.ProcessZaloPayCallbackAsync(queryParams, cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet("{orderCode:int}/status")]
        public async Task<IActionResult> GetPaymentStatus(int orderCode, CancellationToken cancellationToken)
        {
            var result = await _paymentService.GetPaymentStatusAsync(orderCode, cancellationToken);
            return this.FromResult(result);
        }
    }
}

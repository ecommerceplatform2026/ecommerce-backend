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

        [HttpGet("vnpay/callback")]
        public async Task<IActionResult> VnPayCallback(CancellationToken cancellationToken)
        {
            var queryParams = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
            var result = await _paymentService.ProcessVnPayCallbackAsync(queryParams, cancellationToken);

            return Ok(new
            {
                RspCode = result.IsSuccess ? "00" : "99",
                Message = result.IsSuccess ? "success" : "unknown error"
            });
        }

        [HttpPost("momo/callback")]
        public async Task<IActionResult> MomoCallback(
            [FromBody] Dictionary<string, string> body,
            CancellationToken cancellationToken)
        {
            var result = await _paymentService.ProcessMomoCallbackAsync(body, cancellationToken);

            return Ok(new
            {
                resultCode = result.IsSuccess ? 0 : 99,
                message = result.IsSuccess ? "OK" : "Error"
            });
        }

        [HttpPost("zalopay/callback")]
        public async Task<IActionResult> ZaloPayCallback(
            [FromBody] Dictionary<string, string> body,
            CancellationToken cancellationToken)
        {
            var result = await _paymentService.ProcessZaloPayCallbackAsync(body, cancellationToken);

            return Ok(new
            {
                return_code = result.IsSuccess ? 1 : 0,
                return_message = result.IsSuccess ? "success" : "failed"
            });
        }

        [HttpGet("{orderCode:int}/status")]
        public async Task<IActionResult> GetPaymentStatus(int orderCode, CancellationToken cancellationToken)
        {
            var result = await _paymentService.GetPaymentStatusAsync(orderCode, cancellationToken);
            return this.FromResult(result);
        }
    }
}
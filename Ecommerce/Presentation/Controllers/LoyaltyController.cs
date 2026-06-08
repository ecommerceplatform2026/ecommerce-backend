using Application.DTOs.Loyalty;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;

namespace Presentation.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/loyalty")]
    public sealed class LoyaltyController : ControllerBase
    {
        private readonly ILoyaltyService _loyaltyService;

        public LoyaltyController(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        }

        /// <summary>
        /// Get current loyalty points balance and VND equivalent for authenticated user
        /// </summary>
        [HttpGet("balance")]
        public async Task<IActionResult> GetLoyaltyBalance(CancellationToken cancellationToken)
        {
            var result = await _loyaltyService.GetLoyaltyBalanceAsync(cancellationToken);
            return this.FromResult(result);
        }

        /// <summary>
        /// Get paginated transaction history for authenticated user
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactionHistory(
            [FromQuery] GetLoyaltyTransactionsRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await _loyaltyService.GetTransactionHistoryAsync(request, cancellationToken);
            return this.FromResult(result);
        }
    }
}

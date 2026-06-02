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
        private readonly ICurrentUserService _currentUserService;

        public LoyaltyController(ILoyaltyService loyaltyService, ICurrentUserService currentUserService)
        {
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        /// <summary>
        /// Get current loyalty points balance and VND equivalent for authenticated user
        /// </summary>
        [HttpGet("balance")]
        public async Task<IActionResult> GetLoyaltyBalance(CancellationToken cancellationToken)
        {
            var userId = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var result = await _loyaltyService.GetLoyaltyBalanceAsync(userId, cancellationToken);
            return this.FromResult(result);
        }

        /// <summary>
        /// Get paginated transaction history for authenticated user
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactionHistory(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            // Validate pagination parameters
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1 || pageSize > 100)
                pageSize = 10;

            var result = await _loyaltyService.GetTransactionHistoryAsync(userId, pageNumber, pageSize, cancellationToken);
            return this.FromResult(result);
        }
    }
}

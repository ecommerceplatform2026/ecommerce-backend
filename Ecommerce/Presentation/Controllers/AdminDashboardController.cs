using Application.DTOs.Dashboard;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminDashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public AdminDashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] DashboardRequest request, CancellationToken cancellationToken)
        {
            if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate.Value > request.EndDate.Value)
            {
                return BadRequest(new Common.Responses.ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { "StartDate cannot be after EndDate." }
                });
            }

            var result = await _dashboardService.GetDashboardSummaryAsync(request, cancellationToken);
            return this.FromResult(result);
        }
    }
}

using Application.Common.Response;
using Application.DTOs.Dashboard;

namespace Application.Interfaces.Services
{
    public interface IDashboardService
    {
        Task<Result<DashboardSummaryResponse>> GetDashboardSummaryAsync(DashboardRequest request, CancellationToken cancellationToken);
    }
}

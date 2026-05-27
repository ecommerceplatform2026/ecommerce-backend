using Application.Common.Response;
using Application.DTOs.Dashboard;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository ?? throw new ArgumentNullException(nameof(dashboardRepository));
        }

        public async Task<Result<DashboardSummaryResponse>> GetDashboardSummaryAsync(DashboardRequest request, CancellationToken cancellationToken = default)
        {
            var summary = await _dashboardRepository.GetDashboardSummaryAsync(request, cancellationToken);
            return Result<DashboardSummaryResponse>.Success(summary);
        }
    }
}

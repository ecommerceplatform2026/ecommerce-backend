using Application.Common.Response;
using Application.DTOs.Dashboard;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using System;
using System.Collections.Generic;
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

        public async Task<Result<List<RevenueTrendResponse>>> GetRevenueTrendAsync(DashboardRequest request, CancellationToken cancellationToken = default)
        {
            var trend = await _dashboardRepository.GetRevenueTrendAsync(request, cancellationToken);
            return Result<List<RevenueTrendResponse>>.Success(trend);
        }

        public async Task<Result<List<PaymentMethodSummaryResponse>>> GetPaymentMethodSummaryAsync(DashboardRequest request, CancellationToken cancellationToken = default)
        {
            var summary = await _dashboardRepository.GetPaymentMethodSummaryAsync(request, cancellationToken);
            return Result<List<PaymentMethodSummaryResponse>>.Success(summary);
        }
    }
}

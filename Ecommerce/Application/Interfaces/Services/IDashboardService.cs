using Application.Common.Response;
using Application.DTOs.Dashboard;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IDashboardService
    {
        Task<Result<DashboardSummaryResponse>> GetDashboardSummaryAsync(DashboardRequest request, CancellationToken cancellationToken = default);
        Task<Result<List<RevenueTrendResponse>>> GetRevenueTrendAsync(DashboardRequest request, CancellationToken cancellationToken = default);
        Task<Result<List<PaymentMethodSummaryResponse>>> GetPaymentMethodSummaryAsync(DashboardRequest request, CancellationToken cancellationToken = default);
    }
}

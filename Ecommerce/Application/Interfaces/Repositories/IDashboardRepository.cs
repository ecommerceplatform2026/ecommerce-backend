using Application.DTOs.Dashboard;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IDashboardRepository
    {
        Task<DashboardSummaryResponse> GetDashboardSummaryAsync(DashboardRequest request, CancellationToken cancellationToken = default);
        Task<List<RevenueTrendResponse>> GetRevenueTrendAsync(DashboardRequest request, CancellationToken cancellationToken = default);
        Task<List<PaymentMethodSummaryResponse>> GetPaymentMethodSummaryAsync(DashboardRequest request, CancellationToken cancellationToken = default);
    }
}

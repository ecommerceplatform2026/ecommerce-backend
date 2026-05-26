using Application.DTOs.Dashboard;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IDashboardRepository
    {
        Task<DashboardSummaryResponse> GetDashboardSummaryAsync(DashboardRequest request, CancellationToken cancellationToken = default);
    }
}

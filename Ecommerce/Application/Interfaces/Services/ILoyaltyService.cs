using Application.Common.Response;
using Application.DTOs.Loyalty;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface ILoyaltyService
    {
        Task<Result<int>> AwardPendingPointsForDeliveredOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task<Result<int>> CompletePendingTransactionsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task<Result<GetLoyaltyBalanceResponse>> GetLoyaltyBalanceAsync(CancellationToken cancellationToken = default);
        Task<Result<PagedResult<GetLoyaltyTransactionResponse>>> GetTransactionHistoryAsync(GetLoyaltyTransactionsRequest request, CancellationToken cancellationToken = default);
    }
}

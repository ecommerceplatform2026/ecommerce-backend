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
        Task<Result<RedeemPointsResponse>> RedeemPointsAtCheckoutAsync(RedeemPointsRequest request, CancellationToken cancellationToken = default);
        Task<Result<int>> CompleteRedeemedPointsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task<Result<int>> RefundRedeemedPointsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task<Result<int>> ReverseEarnedPointsForReturnedOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    }
}

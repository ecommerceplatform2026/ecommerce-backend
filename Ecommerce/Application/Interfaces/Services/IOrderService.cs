using Application.Common.Response;
using Application.DTOs.Order;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<Result<PagedResult<OrderResponse>>> GetMyOrdersAsync(GetOrdersRequest request, CancellationToken cancellationToken = default);
        Task<Result<OrderResponse>> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result<CancelOrderResponse>> CancelOrderAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result<CompleteOrderResponse>> CompleteOrderAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result<ReturnOrderResponse>> ReturnOrderAsync(Guid id, CancellationToken cancellationToken = default);
    }
}

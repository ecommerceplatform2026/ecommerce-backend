using Application.Common.Response;
using Application.DTOs.Order;

namespace Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<Result<PagedResult<OrderResponse>>> GetMyOrdersAsync(GetOrdersRequest request, CancellationToken cancellationToken = default);
    }
}

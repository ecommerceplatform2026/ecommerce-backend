using Application.Common.Response;
using Domain.Enums;

namespace Application.DTOs.Order
{
    public sealed class GetOrdersRequest : PagingRequest
    {
        public OrderStatus? Status { get; set; }
    }
}

using Application.Common.Response;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Order
{
    public sealed class GetOrdersRequest : PagingRequest
    {
        [EnumDataType(typeof(OrderStatus), ErrorMessage = "Invalid OrderStatus.")]
        public OrderStatus? Status { get; set; }
    }
}

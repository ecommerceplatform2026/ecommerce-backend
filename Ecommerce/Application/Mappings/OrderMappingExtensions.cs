using Application.DTOs.Order;
using Domain.Entities;

namespace Application.Mappings
{
    public static class OrderMappingExtensions
    {
        public static OrderResponse ToOrderResponse(this Order order)
        {
            return new OrderResponse(
                order.Id,
                order.OrderCode,
                order.TotalAmount,
                order.Status,
                order.PaymentMethod,
                order.CreatedAt,
                order.OrderItems.Select(oi => oi.ToOrderItemResponse()).ToList()
            );
        }

        public static OrderItemResponse ToOrderItemResponse(this OrderItem item)
        {
            return new OrderItemResponse(
                item.Id,
                item.ProductVariantId,
                item.Quantity,
                item.Price,
                item.ProductSnapshot
            );
        }
    }
}

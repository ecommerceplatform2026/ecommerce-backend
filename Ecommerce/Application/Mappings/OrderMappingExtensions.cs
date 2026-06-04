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
                order.TotalAmount.Amount,
                order.Status,
                order.PaymentMethod,
                order.CreatedAt,
                order.OrderItems != null
                    ? order.OrderItems.Select(oi => oi.ToOrderItemResponse()).ToList()
                    : new List<OrderItemResponse>(),
                order.Delivery != null
                    ? new TrackingInfo(order.Delivery.TrackingCode, order.Delivery.CarrierCode, order.Delivery.Status)
                    : null
            );
        }

        public static OrderItemResponse ToOrderItemResponse(this OrderItem item)
        {
            return new OrderItemResponse(
                item.Id,
                item.ProductVariantId,
                item.Quantity,
                item.Price.Amount,
                item.ProductSnapshot
            );
        }
    }
}

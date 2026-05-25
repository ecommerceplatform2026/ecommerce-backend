using Domain.Enums;

namespace Application.DTOs.Order
{
    public sealed record OrderResponse(
        Guid Id,
        int OrderCode,
        long TotalAmount,
        OrderStatus Status,
        PaymentMethod PaymentMethod,
        DateTime CreatedAt,
        List<OrderItemResponse> Items);

    public sealed record OrderItemResponse(
        Guid Id,
        Guid ProductVariantId,
        int Quantity,
        long Price,
        string ProductSnapshot);
}

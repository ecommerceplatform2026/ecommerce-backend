using Domain.Enums;

namespace Application.DTOs.Checkout
{
    public sealed record CheckoutResponse(
        Guid OrderId,
        int OrderCode,
        long TotalAmount,
        OrderStatus Status,
        PaymentMethod PaymentMethod,
        List<CheckoutItemResponse> Items,
        string? CheckoutUrl = null,
        string? PaymentLinkId = null);

    public sealed record CheckoutItemResponse(
        Guid OrderItemId,
        Guid ProductVariantId,
        int Quantity,
        long Price,
        string ProductSnapshot);
}

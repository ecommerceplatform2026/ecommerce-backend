using Application.DTOs.Loyalty;
using Domain.Enums;

namespace Application.DTOs.Order
{
    public sealed record OrderResponse(
        Guid Id,
        int OrderCode,
        long TotalAmount,
        long DiscountAmount,
        long PaidAmount,
        OrderStatus Status,
        PaymentMethod PaymentMethod,
        DateTime CreatedAt,
        List<OrderItemResponse> Items,
        TrackingInfo? Tracking = null,
        List<GetLoyaltyTransactionResponse>? LoyaltyTransactions = null);

    public sealed record TrackingInfo(
        string TrackingCode,
        string CarrierCode,
        DeliveryStatus Status);

    public sealed record OrderItemResponse(
        Guid Id,
        Guid ProductVariantId,
        int Quantity,
        long Price,
        string ProductSnapshot);
}

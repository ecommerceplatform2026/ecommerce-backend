namespace Application.DTOs.Delivery
{
    public sealed record ShipmentResponse(
        string TrackingCode,
        string CarrierOrderCode,
        long ShippingFee,
        string? ExpectedDeliveryTime);
}

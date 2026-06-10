namespace Application.DTOs.Delivery
{
    public sealed class ShipmentResponse
    {
        public string TrackingCode { get; }
        public string CarrierOrderCode { get; }
        public long ShippingFee { get; }
        public string? ExpectedDeliveryTime { get; }
        public string? SortCode { get; }
        public string? TransportType { get; }

        public ShipmentResponse(
            string trackingCode,
            string carrierOrderCode,
            long shippingFee,
            string? expectedDeliveryTime,
            string? sortCode = null,
            string? transportType = null)
        {
            TrackingCode = trackingCode;
            CarrierOrderCode = carrierOrderCode;
            ShippingFee = shippingFee;
            ExpectedDeliveryTime = expectedDeliveryTime;
            SortCode = sortCode;
            TransportType = transportType;
        }
    }
}

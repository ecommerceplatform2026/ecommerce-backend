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
        public ShipmentFeeBreakdown? FeeBreakdown { get; }

        public ShipmentResponse(
            string trackingCode,
            string carrierOrderCode,
            long shippingFee,
            string? expectedDeliveryTime,
            string? sortCode = null,
            string? transportType = null,
            ShipmentFeeBreakdown? feeBreakdown = null)
        {
            TrackingCode = trackingCode;
            CarrierOrderCode = carrierOrderCode;
            ShippingFee = shippingFee;
            ExpectedDeliveryTime = expectedDeliveryTime;
            SortCode = sortCode;
            TransportType = transportType;
            FeeBreakdown = feeBreakdown;
        }
    }

    public sealed class ShipmentFeeBreakdown
    {
        public long MainService { get; }
        public long Insurance { get; }
        public long CodFee { get; }
        public long StationDo { get; }
        public long StationPu { get; }
        public long Return { get; }
        public long R2s { get; }
        public long ReturnAgain { get; }
        public long Coupon { get; }
        public long DocumentReturn { get; }
        public long DoubleCheck { get; }
        public long DoubleCheckDeliver { get; }
        public long PickRemoteAreasFee { get; }
        public long DeliverRemoteAreasFee { get; }
        public long PickRemoteAreasFeeReturn { get; }
        public long DeliverRemoteAreasFeeReturn { get; }
        public long CodFailedFee { get; }
        public long ChangeToAddressFee { get; }
        public long ChangeReturnAddressFee { get; }

        public ShipmentFeeBreakdown(
            long mainService, long insurance, long codFee,
            long stationDo, long stationPu, long returns,
            long r2s, long returnAgain, long coupon,
            long documentReturn, long doubleCheck, long doubleCheckDeliver,
            long pickRemoteAreasFee, long deliverRemoteAreasFee,
            long pickRemoteAreasFeeReturn, long deliverRemoteAreasFeeReturn,
            long codFailedFee, long changeToAddressFee, long changeReturnAddressFee)
        {
            MainService = mainService;
            Insurance = insurance;
            CodFee = codFee;
            StationDo = stationDo;
            StationPu = stationPu;
            Return = returns;
            R2s = r2s;
            ReturnAgain = returnAgain;
            Coupon = coupon;
            DocumentReturn = documentReturn;
            DoubleCheck = doubleCheck;
            DoubleCheckDeliver = doubleCheckDeliver;
            PickRemoteAreasFee = pickRemoteAreasFee;
            DeliverRemoteAreasFee = deliverRemoteAreasFee;
            PickRemoteAreasFeeReturn = pickRemoteAreasFeeReturn;
            DeliverRemoteAreasFeeReturn = deliverRemoteAreasFeeReturn;
            CodFailedFee = codFailedFee;
            ChangeToAddressFee = changeToAddressFee;
            ChangeReturnAddressFee = changeReturnAddressFee;
        }
    }
}

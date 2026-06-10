using Domain.Common;
using Domain.Enums;
using System;

namespace Domain.Entities
{
    public class Delivery : BaseEntity
    {
        public Guid OrderId { get; private set; }
        public string CarrierCode { get; private set; } = string.Empty;
        public string TrackingCode { get; private set; } = string.Empty;
        public string? CarrierOrderCode { get; private set; }
        public string ToName { get; private set; } = string.Empty;
        public string ToPhone { get; private set; } = string.Empty;
        public string ToAddress { get; private set; } = string.Empty;
        public string Province { get; private set; } = string.Empty;
        public string District { get; private set; } = string.Empty;
        public string Ward { get; private set; } = string.Empty;
        public int Weight { get; private set; }
        public int Length { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public long CodAmount { get; private set; }
        public long InsuranceValue { get; private set; }
        public long ShippingFee { get; private set; }
        public string? Note { get; private set; }
        public DeliveryStatus Status { get; private set; }

        public Order? Order { get; set; }

        private Delivery() { }

        private Delivery(
            Guid orderId,
            string carrierCode,
            string toName, string toPhone, string toAddress,
            string province, string district, string ward,
            int weight, int length, int width, int height,
            long codAmount, long insuranceValue,
            string? note)
        {
            if (orderId == Guid.Empty)
                throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));
            if (string.IsNullOrWhiteSpace(carrierCode))
                throw new ArgumentException("Carrier code cannot be empty.", nameof(carrierCode));

            OrderId = orderId;
            CarrierCode = carrierCode;
            ToName = toName;
            ToPhone = toPhone;
            ToAddress = toAddress;
            Province = province;
            District = district;
            Ward = ward;
            Weight = weight;
            Length = length;
            Width = width;
            Height = height;
            CodAmount = codAmount;
            InsuranceValue = insuranceValue;
            Note = note;
            Status = DeliveryStatus.Pending;
        }

        public static Delivery Create(
            Guid orderId,
            string carrierCode,
            string toName, string toPhone, string toAddress,
            string province, string district, string ward,
            int weight, int length, int width, int height,
            long codAmount, long insuranceValue,
            string? note)
        {
            return new Delivery(orderId, carrierCode,
                toName, toPhone, toAddress,
                province, district, ward,
                weight, length, width, height,
                codAmount, insuranceValue, note);
        }

        public void MarkShipmentCreated(string trackingCode, string? carrierOrderCode, long shippingFee)
        {
            if (Status != DeliveryStatus.Pending)
                throw new InvalidOperationException($"Cannot mark shipment as created when status is {Status}.");

            TrackingCode = trackingCode ?? throw new ArgumentNullException(nameof(trackingCode));
            CarrierOrderCode = carrierOrderCode;
            ShippingFee = shippingFee;
            Status = DeliveryStatus.Created;
        }

        public void MarkFailed(string? errorMessage)
        {
            Status = DeliveryStatus.Failed;
            Note = errorMessage;
        }
    }
}

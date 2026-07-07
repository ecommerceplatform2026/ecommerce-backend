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

        public void MarkPickedUp()
        {
            Status = DeliveryStatus.PickedUp;
        }

        public void MarkInTransit()
        {
            Status = DeliveryStatus.InTransit;
        }

        public void MarkOutForDelivery()
        {
            Status = DeliveryStatus.OutForDelivery;
        }

        public void MarkDelivered()
        {
            if (Status == DeliveryStatus.Delivered)
                return;
            Status = DeliveryStatus.Delivered;
        }

        public void MarkCancelled()
        {
            Status = DeliveryStatus.Cancelled;
        }

        public void MarkReturned()
        {
            Status = DeliveryStatus.Returned;
        }

        public void MarkException(string? errorMessage)
        {
            Status = DeliveryStatus.Exception;
            if (!string.IsNullOrWhiteSpace(errorMessage))
                Note = errorMessage;
        }

        public void ResetForRetry()
        {
            if (Status != DeliveryStatus.Exception)
                throw new InvalidOperationException($"Cannot reset for retry when status is {Status}. Only Exception deliveries can be retried.");

            var history = Note ?? string.Empty;
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            Note = $"[Retry at {timestamp}] {history}".Trim();

            TrackingCode = string.Empty;
            CarrierOrderCode = null;
            ShippingFee = 0;
            Status = DeliveryStatus.Pending;
        }

        public void UpdateWeight(int newWeight)
        {
            if (newWeight <= 0)
                return;
            Weight = newWeight;
        }

        public void UpdateCodAmount(long newCodAmount)
        {
            if (newCodAmount < 0)
                return;
            CodAmount = newCodAmount;
        }

        public void UpdateShippingFee(long newShippingFee)
        {
            if (newShippingFee < 0)
                return;
            ShippingFee = newShippingFee;
        }
    }
}

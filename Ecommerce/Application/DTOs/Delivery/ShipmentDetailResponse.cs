using Domain.Enums;
using System;

namespace Application.DTOs.Delivery
{
    public class ShipmentDetailResponse
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string CarrierCode { get; set; } = string.Empty;
        public string TrackingCode { get; set; } = string.Empty;
        public string? CarrierOrderCode { get; set; }
        public string ToName { get; set; } = string.Empty;
        public string ToPhone { get; set; } = string.Empty;
        public string ToAddress { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public int Weight { get; set; }
        public long CodAmount { get; set; }
        public long InsuranceValue { get; set; }
        public long ShippingFee { get; set; }
        public string? Note { get; set; }
        public DeliveryStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

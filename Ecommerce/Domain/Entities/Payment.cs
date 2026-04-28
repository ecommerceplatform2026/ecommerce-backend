using Domain.Common;
using Domain.Enums;
using System;

namespace Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Guid OrderId { get; set; }
        public string PaymentLinkId { get; set; } = string.Empty;
        public int OrderCode { get; set; }
        public string? CheckoutUrl { get; set; }
        public long Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public PaymentStatus Status { get; set; }
        public DateTime? PaidAt { get; set; }

        public Order? Order { get; set; }
    }
}

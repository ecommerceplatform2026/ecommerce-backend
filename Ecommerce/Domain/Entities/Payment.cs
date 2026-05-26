using Domain.Common;
using Domain.Enums;
using System;

namespace Domain.Entities
{
    public class Payment : BaseEntity
    {
        public Guid OrderId { get; private set; }
        public string PaymentLinkId { get; private set; } = string.Empty;
        public int OrderCode { get; private set; }
        public string? CheckoutUrl { get; private set; }
        public Money Amount { get; private set; } = null!;
        public PaymentStatus Status { get; private set; }
        public DateTime? PaidAt { get; private set; }

        public Order? Order { get; set; }

        private Payment() { }

        private Payment(Guid orderId, int orderCode, Money amount, string paymentLinkId, string? checkoutUrl)
        {
            if (orderId == Guid.Empty)
                throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));
            if (orderCode <= 0)
                throw new ArgumentException("Order code must be greater than zero.", nameof(orderCode));

            OrderId = orderId;
            OrderCode = orderCode;
            Amount = amount ?? throw new ArgumentNullException(nameof(amount));
            PaymentLinkId = paymentLinkId ?? string.Empty;
            CheckoutUrl = checkoutUrl;
            Status = PaymentStatus.Pending;
        }

        public static Payment Create(Guid orderId, int orderCode, Money amount, string paymentLinkId, string? checkoutUrl)
        {
            return new Payment(orderId, orderCode, amount, paymentLinkId, checkoutUrl);
        }

        public void Complete(DateTime paidAt)
        {
            if (Status != PaymentStatus.Pending)
                throw new InvalidOperationException($"Cannot complete a payment in '{Status}' status.");

            Status = PaymentStatus.Success;
            PaidAt = paidAt;

            if (Order != null)
            {
                AddDomainEvent(new Events.PaymentConfirmedDomainEvent(Order, this));
            }
        }

        public void Fail()
        {
            if (Status != PaymentStatus.Pending)
                throw new InvalidOperationException($"Cannot fail a payment in '{Status}' status.");

            Status = PaymentStatus.Failed;
        }
    }
}

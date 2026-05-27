using Domain.Common;
using Domain.Entities;
using System;

namespace Domain.Events
{
    public class PaymentConfirmedDomainEvent : IDomainEvent
    {
        public Order Order { get; }
        public Payment Payment { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public PaymentConfirmedDomainEvent(Order order, Payment payment)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
            Payment = payment ?? throw new ArgumentNullException(nameof(payment));
        }
    }
}

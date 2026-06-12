using Domain.Common;
using Domain.Entities;
using System;

namespace Domain.Events
{
    public class OrderConfirmedDomainEvent : IDomainEvent
    {
        public Order Order { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public OrderConfirmedDomainEvent(Order order)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
        }
    }
}

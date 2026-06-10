using Domain.Common;
using Domain.Entities;
using System;

namespace Domain.Events
{
    public class OrderCancelledDomainEvent : IDomainEvent
    {
        public Order Order { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public OrderCancelledDomainEvent(Order order)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
        }
    }
}

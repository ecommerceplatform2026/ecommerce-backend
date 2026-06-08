using Domain.Common;
using Domain.Entities;
using System;

namespace Domain.Events
{
    public class OrderCompletedDomainEvent : IDomainEvent
    {
        public Order Order { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public OrderCompletedDomainEvent(Order order)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
        }
    }
}

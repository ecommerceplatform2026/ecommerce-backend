using Domain.Common;
using Domain.Entities;
using System;

namespace Domain.Events
{
    public class OrderReturnedDomainEvent : IDomainEvent
    {
        public Order Order { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public OrderReturnedDomainEvent(Order order)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
        }
    }
}

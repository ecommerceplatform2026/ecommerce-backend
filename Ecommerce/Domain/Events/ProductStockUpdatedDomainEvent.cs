using Domain.Common;
using System;

namespace Domain.Events
{
    public class ProductStockUpdatedDomainEvent : IDomainEvent
    {
        public Guid ProductId { get; }
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        public ProductStockUpdatedDomainEvent(Guid productId)
        {
            ProductId = productId;
        }
    }
}

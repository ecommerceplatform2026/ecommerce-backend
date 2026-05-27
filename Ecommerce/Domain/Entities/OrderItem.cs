using Domain.Common;
using System;

namespace Domain.Entities
{
    public class OrderItem : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Guid ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public long Price { get; set; }
        public string ProductSnapshot { get; set; } = string.Empty;

        public Order? Order { get; set; }
        public ProductVariant? ProductVariant { get; set; }
    }
}

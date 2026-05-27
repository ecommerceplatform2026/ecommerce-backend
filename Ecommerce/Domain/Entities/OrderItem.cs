using Domain.Common;
using System;

namespace Domain.Entities
{
    public class OrderItem : BaseEntity
    {
        public Guid OrderId { get; private set; }
        public Guid ProductVariantId { get; private set; }
        public int Quantity { get; private set; }
        public Money Price { get; private set; } = null!;
        public string ProductSnapshot { get; private set; } = string.Empty;

        public Order? Order { get; set; }
        public ProductVariant? ProductVariant { get; set; }

        private OrderItem() { }

        private OrderItem(Guid orderId, Guid productVariantId, int quantity, Money price, string productSnapshot)
        {
            OrderId = orderId;
            ProductVariantId = productVariantId;
            Quantity = quantity;
            Price = price ?? throw new ArgumentNullException(nameof(price));
            ProductSnapshot = productSnapshot;
        }

        internal static OrderItem Create(Guid orderId, Guid productVariantId, int quantity, Money price, string productSnapshot)
        {
            if (productVariantId == Guid.Empty)
                throw new ArgumentException("Product variant ID cannot be empty.", nameof(productVariantId));
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
            if (price == null)
                throw new ArgumentNullException(nameof(price));
            if (string.IsNullOrWhiteSpace(productSnapshot))
                throw new ArgumentException("Product snapshot cannot be empty.", nameof(productSnapshot));

            return new OrderItem(orderId, productVariantId, quantity, price, productSnapshot);
        }
    }
}

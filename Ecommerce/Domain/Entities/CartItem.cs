using Domain.Common;
using System;

namespace Domain.Entities
{
    public class CartItem : BaseEntity
    {
        public Guid? UserId { get; set; }
        public Guid ProductVariantId { get; set; }
        public int Quantity { get; set; }

        public User? User { get; set; }
        public ProductVariant? ProductVariant { get; set; }
    }
}

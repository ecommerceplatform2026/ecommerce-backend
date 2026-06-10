using Domain.Common;
using System;

namespace Domain.Entities
{
    public class WishlistItem : BaseEntity
    {
        public Guid? UserId { get; set; }
        public Guid ProductVariantId { get; set; }

        public virtual User? User { get; set; }
        public virtual ProductVariant? ProductVariant { get; set; }

        // EF Core constructor
        public WishlistItem() { }

        private WishlistItem(Guid? userId, Guid productVariantId)
        {
            UserId = userId;
            ProductVariantId = productVariantId;
        }

        public static WishlistItem Create(Guid? userId, Guid productVariantId)
        {
            return new WishlistItem(userId, productVariantId);
        }
    }
}

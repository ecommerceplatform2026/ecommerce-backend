using Domain.Common;
using System;

namespace Domain.Entities
{
    public class RecentlyViewedProduct : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;

        // EF Core constructor
        public RecentlyViewedProduct() { }

        private RecentlyViewedProduct(Guid userId, Guid productId)
        {
            UserId = userId;
            ProductId = productId;
        }

        public static RecentlyViewedProduct Create(Guid userId, Guid productId)
        {
            return new RecentlyViewedProduct(userId, productId);
        }
    }
}

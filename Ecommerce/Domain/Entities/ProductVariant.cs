using Domain.Common;

namespace Domain.Entities
{
    public class ProductVariant : BaseEntity
    {
        public Guid ProductId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Size { get; set; }
        public int Stock { get; set; }
        public long Price { get; set; }

        public Product Product { get; set; } = null!;
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}

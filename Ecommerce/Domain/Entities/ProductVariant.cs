using Domain.Common;

namespace Domain.Entities
{
    public class ProductVariant : BaseEntity
    {
        public Guid ProductId { get; private set; }
        public Sku SKU { get; private set; } = null!;
        public string? Color { get; private set; }
        public string? Size { get; private set; }
        public long Stock { get; private set; }
        public long LowStockThreshold { get; private set; }
        public Money Price { get; private set; } = null!;
        public int Weight { get; private set; }
        public int Length { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public Product Product { get; set; } = null!;
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();

        private ProductVariant() { }

        private ProductVariant(Guid productId, Sku sku, string? color, string? size, long stock, Money price, long lowStockThreshold = 5, int weight = 1, int length = 1, int width = 1, int height = 1)
        {
            ProductId = productId;
            SKU = sku ?? throw new ArgumentNullException(nameof(sku));
            Color = color?.Trim();
            Size = size?.Trim();
            Stock = EnsureNonNegative(stock, nameof(stock));
            Price = price ?? throw new ArgumentNullException(nameof(price));
            LowStockThreshold = EnsureNonNegative(lowStockThreshold, nameof(lowStockThreshold));
            Weight = Math.Max(0, weight);
            Length = Math.Max(0, length);
            Width = Math.Max(0, width);
            Height = Math.Max(0, height);
        }

        public static ProductVariant Create(Guid productId, Sku sku, string? color, string? size, long stock, Money price, long lowStockThreshold = 5, int weight = 1, int length = 1, int width = 1, int height = 1)
        {
            return new ProductVariant(productId, sku, color, size, stock, price, lowStockThreshold, weight, length, width, height);
        }

        public void Update(Sku sku, string? color, string? size, Money price, long lowStockThreshold, int weight = 1, int length = 1, int width = 1, int height = 1)
        {
            SKU = sku ?? throw new ArgumentNullException(nameof(sku));
            Color = color?.Trim();
            Size = size?.Trim();
            Price = price ?? throw new ArgumentNullException(nameof(price));
            LowStockThreshold = EnsureNonNegative(lowStockThreshold, nameof(lowStockThreshold));
            Weight = Math.Max(0, weight);
            Length = Math.Max(0, length);
            Width = Math.Max(0, width);
            Height = Math.Max(0, height);
        }

        public void UpdateStock(long newStock)
        {
            Stock = EnsureNonNegative(newStock, nameof(Stock));
            AddDomainEvent(new Events.ProductStockUpdatedDomainEvent(ProductId));
        }

        public bool IsLowStock() => Stock <= LowStockThreshold;
        public bool IsOutOfStock() => Stock <= 0;

        private static long EnsureNonNegative(long value, string paramName)
        {
            if (value < 0)
                throw new ArgumentException($"{paramName} must be non-negative.", paramName);
            return value;
        }
    }
}

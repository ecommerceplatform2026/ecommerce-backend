using Domain.Common;

namespace Domain.Entities
{
    public class ProductVariant : BaseEntity
    {
        public Guid ProductId { get; private set; }
        public string SKU { get; private set; } = string.Empty;
        public string? Color { get; private set; }
        public string? Size { get; private set; }
        public long Stock { get; private set; }
        public long LowStockThreshold { get; private set; }
        public long Price { get; private set; }

        public Product Product { get; set; } = null!;
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        private ProductVariant() { }

        private ProductVariant(Guid productId, string sku, string? color, string? size, long stock, long price, long lowStockThreshold = 5)
        {
            ProductId = productId;
            SKU = NormalizeRequired(sku);
            Color = color?.Trim();
            Size = size?.Trim();
            Stock = EnsureNonNegative(stock, nameof(stock));
            Price = EnsureNonNegative(price, nameof(price));
            LowStockThreshold = EnsureNonNegative(lowStockThreshold, nameof(lowStockThreshold));
        }

        public static ProductVariant Create(Guid productId, string sku, string? color, string? size, long stock, long price, long lowStockThreshold = 5)
        {
            return new ProductVariant(productId, sku, color, size, stock, price, lowStockThreshold);
        }

        public void Update(string sku, string? color, string? size, long price, long lowStockThreshold)
        {
            SKU = NormalizeRequired(sku);
            Color = color?.Trim();
            Size = size?.Trim();
            Price = EnsureNonNegative(price, nameof(price));
            LowStockThreshold = EnsureNonNegative(lowStockThreshold, nameof(lowStockThreshold));
        }

        public void UpdateStock(long newStock)
        {
            Stock = EnsureNonNegative(newStock, nameof(Stock));
        }

        public bool IsLowStock() => Stock <= LowStockThreshold;
        public bool IsOutOfStock() => Stock <= 0;

        private static string NormalizeRequired(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));

            return value.Trim();
        }

        private static long EnsureNonNegative(long value, string paramName)
        {
            if (value < 0)
                throw new ArgumentException($"{paramName} must be non-negative.", paramName);
            return value;
        }
    }
}

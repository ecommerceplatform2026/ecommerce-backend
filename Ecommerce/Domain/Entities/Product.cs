using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class Product : BaseEntity
    {
        public Guid CategoryId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public string? Material { get; private set; }
        public Money BasePrice { get; private set; } = null!;
        public ProductStatus Status { get; private set; }

        public Category Category { get; set; } = null!;
        public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
        public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        private Product() { }

        private Product(Guid categoryId, string name, string? description, string? material, Money basePrice, ProductStatus status)
        {
            CategoryId = EnsureNotEmpty(categoryId);
            Name = NormalizeRequired(name);
            Description = description;
            Material = material;
            BasePrice = basePrice ?? throw new ArgumentNullException(nameof(basePrice));
            Status = status;
        }

        public static Product Create(Guid categoryId, string name, string? description, string? material, Money basePrice, ProductStatus status)
        {
            return new Product(categoryId, name, description, material, basePrice, status);
        }

        public void Update(Guid categoryId, string name, string? description, string? material, Money basePrice, ProductStatus status)
        {
            CategoryId = EnsureNotEmpty(categoryId);
            Name = NormalizeRequired(name);
            Description = description;
            Material = material;
            BasePrice = basePrice ?? throw new ArgumentNullException(nameof(basePrice));
            Status = status;
        }

        public void Deactivate() => Status = ProductStatus.Inactive;

        public void AddVariant(string sku, string? color, string? size, long stock, Money price, long lowStockThreshold)
        {
            sku = NormalizeRequired(sku);

            if (ProductVariants.Any(v => v.SKU.Value.Equals(sku, StringComparison.OrdinalIgnoreCase) && !v.IsDeleted))
                throw new InvalidOperationException($"Variant with SKU '{sku}' already exists for this product.");

            var variant = ProductVariant.Create(Id, new Sku(sku), color, size, stock, price, lowStockThreshold);
            ProductVariants.Add(variant);
        }

        public void UpdateVariant(Guid variantId, string sku, string? color, string? size, long stock, Money price, long lowStockThreshold)
        {
            sku = NormalizeRequired(sku);

            var variant = ProductVariants.FirstOrDefault(v => v.Id == variantId && !v.IsDeleted);
            if (variant == null)
                throw new KeyNotFoundException("Variant not found.");

            if (ProductVariants.Any(v => v.Id != variantId && v.SKU.Value.Equals(sku, StringComparison.OrdinalIgnoreCase) && !v.IsDeleted))
                throw new InvalidOperationException($"Variant with SKU '{sku}' already exists for this product.");

            variant.Update(new Sku(sku), color, size, price, lowStockThreshold);
            variant.UpdateStock(stock);
        }

        public void RemoveVariant(Guid variantId, string userId)
        {
            var variant = ProductVariants.FirstOrDefault(v => v.Id == variantId && !v.IsDeleted);
            if (variant != null)
            {
                variant.SetDeleted(userId);
            }
        }

        private static string NormalizeRequired(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));

            return value.Trim();
        }



        private static Guid EnsureNotEmpty(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("GUID cannot be empty.", nameof(guid));
            return guid;
        }
    }
}

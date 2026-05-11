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
        public long BasePrice { get; private set; }
        public ProductStatus Status { get; private set; }

        public Category Category { get; set; } = null!;
        public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
        public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        private Product() { }

        private Product(Guid categoryId, string name, string? description, string? material, long basePrice, ProductStatus status)
        {
            CategoryId = EnsureNotEmpty(categoryId);
            Name = NormalizeRequired(name);
            Description = description;
            Material = material;
            BasePrice = EnsureNonNegative(basePrice);
            Status = status;
        }

        public static Product Create(Guid categoryId, string name, string? description, string? material, long basePrice, ProductStatus status)
        {
            return new Product(categoryId, name, description, material, basePrice, status);
        }

        public void Update(Guid categoryId, string name, string? description, string? material, long basePrice, ProductStatus status)
        {
            CategoryId = EnsureNotEmpty(categoryId);
            Name = NormalizeRequired(name);
            Description = description;
            Material = material;
            BasePrice = EnsureNonNegative(basePrice);
            Status = status;
        }

        public void Deactivate() => Status = ProductStatus.Inactive;

        private static string NormalizeRequired(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));

            return value.Trim();
        }

        private static long EnsureNonNegative(long price)
        {
            if (price < 0)
                throw new ArgumentException("Base price must be non-negative.", nameof(price));
            return price;
        }

        private static Guid EnsureNotEmpty(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("GUID cannot be empty.", nameof(guid));
            return guid;
        }
    }
}

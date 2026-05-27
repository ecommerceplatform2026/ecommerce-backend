using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public CategoryStatus Status { get; private set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        private Category(){ }

        private Category(string name)
        {
            Name = NormalizeRequired(name);
            Status = CategoryStatus.Active;
        }
        public static Category Create(string name) => new Category(name);

        public void Update(string name) => Name = NormalizeRequired(name);

        public void Deactivate() => Status = CategoryStatus.Inactive;

        private static string NormalizeRequired(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));

            return value.Trim();
        }
    }
}
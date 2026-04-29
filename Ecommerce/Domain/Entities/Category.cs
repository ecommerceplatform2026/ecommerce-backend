using Domain.Common;
using Domain.Enums;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public CategoryStatus Status { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}

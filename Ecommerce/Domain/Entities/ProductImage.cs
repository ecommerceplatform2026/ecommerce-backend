using Domain.Common;
using System;

namespace Domain.Entities
{
    public class ProductImage : BaseEntity
    {
        public Guid ProductId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        public Product? Product { get; set; }
    }
}

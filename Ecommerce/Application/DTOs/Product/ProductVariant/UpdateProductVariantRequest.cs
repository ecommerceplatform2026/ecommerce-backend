using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Product.ProductVariant
{
    public class UpdateProductVariantRequest
    {
        [Required(ErrorMessage = "SKU is required.")]
        [MaxLength(50, ErrorMessage = "SKU must not exceed 50 characters.")]
        public string SKU { get; set; } = string.Empty;

        public string? Color { get; set; }

        public string? Size { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "Stock must be non-negative.")]
        public long Stock { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "Low stock threshold must be non-negative.")]
        public long LowStockThreshold { get; set; } = 5;

        [Range(0, long.MaxValue, ErrorMessage = "Price must be non-negative.")]
        public long Price { get; set; }
    }
}

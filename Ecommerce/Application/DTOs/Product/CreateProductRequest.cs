using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Product
{
    public class CreateProductRequest
    {
        [Required(ErrorMessage = "Category is required.")]
        public Guid CategoryId { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [MaxLength(200, ErrorMessage = "Product name must not exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(100, ErrorMessage = "Material must not exceed 100 characters.")]
        public string? Material { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "Base price must be a positive value.")]
        public long BasePrice { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public ProductStatus Status { get; set; }
    }
}

using Application.Common.Validations;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Product
{
    public class UpdateProductRequest
    {
        [Required(ErrorMessage = "Category is required.")]
        [NotEmptyGuid(ErrorMessage = "Category is required.")]
        public Guid CategoryId { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [RegularExpression(@".*\S.*", ErrorMessage = "Product name must not be empty or whitespace.")]
        [MaxLength(200, ErrorMessage = "Product name must not exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(100, ErrorMessage = "Material must not exceed 100 characters.")]
        public string? Material { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "Base price must be a non-negative value.")]
        public long BasePrice { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [EnumDataType(typeof(ProductStatus), ErrorMessage = "Invalid Status.")]
        public ProductStatus Status { get; set; }
    }
}

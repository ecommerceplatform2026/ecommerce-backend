using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Category
{
    public class CreateCategoryRequest
    {
        [Required(ErrorMessage = "Category name is required.")]
        [RegularExpression(@".*\S.*", ErrorMessage = "Category name must not be empty or whitespace.")]
        [MaxLength(100, ErrorMessage = "Category name must not exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}
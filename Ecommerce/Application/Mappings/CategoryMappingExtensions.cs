using Application.DTOs.Category;
using Domain.Entities;

namespace Application.Mappings
{
    public static class CategoryMappingExtensions
    {
        public static CategoryResponse ToCategoryResponse(this Category category) => new CategoryResponse(category.Id, category.Name, category.CreatedAt);
    }
}
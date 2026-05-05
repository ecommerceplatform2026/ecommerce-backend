using Application.Common.Response;
using Application.DTOs.Category;

namespace Application.Interfaces.Services
{
    public interface ICategoryService
    {
        Task<Result<List<CategoryResponse>>> GetCategoriesAsync(CancellationToken cancellationToken = default);
        Task<Result<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
        Task<Result<CategoryResponse>> UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    }
}
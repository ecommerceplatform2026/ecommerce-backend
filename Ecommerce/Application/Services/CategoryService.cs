using Application.Common.Response;
using Application.DTOs.Category;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Application.Mappings;
using Domain.Entities;

namespace Application.Services
{
    public sealed class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        public CategoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<CategoryResponse>>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var categories = await _unitOfWork.GetRepository<Category>().GetAllAsync(category => !category.IsDeleted, cancellationToken);

            return Result<List<CategoryResponse>>.Success(categories.OrderBy(category => category.Name).Select(c => c.ToCategoryResponse()).ToList());
        }

        public async Task<Result<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return Result<CategoryResponse>.Failure("Category name is required.");

            var categoryRepository = _unitOfWork.GetRepository<Category>();

            var exists = await categoryRepository.FindAsync(c => c.Name.ToLowerInvariant() == name.ToLowerInvariant() && !c.IsDeleted, cancellationToken: cancellationToken);
            if (exists is not null)
                return Result<CategoryResponse>.Failure("Category name already exists.");

            var category = Category.Create(name);

            await categoryRepository.AddAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CategoryResponse>.Success(category.ToCategoryResponse());
        }

        public async Task<Result<CategoryResponse>> UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return Result<CategoryResponse>.Failure("Category name is required.");

            var categoryRepository = _unitOfWork.GetRepository<Category>();

            var category = await categoryRepository.FindAsync(category => category.Id == categoryId && !category.IsDeleted, asNoTracking: false, cancellationToken: cancellationToken);
            if (category is null)
                return Result<CategoryResponse>.NotFound("Category not found.");

            var nameExists = await categoryRepository.FindAsync(item => item.Id != categoryId && !item.IsDeleted && item.Name.ToLowerInvariant() == name.ToLowerInvariant(), cancellationToken: cancellationToken);
            if (nameExists is not null)
                return Result<CategoryResponse>.Failure("Category name already exists.");

            category.Update(name);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CategoryResponse>.Success(category.ToCategoryResponse());
        }

        public async Task<Result<bool>> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            var categoryRepository = _unitOfWork.GetRepository<Category>();

            var category = await categoryRepository.FindAsync(category => category.Id == categoryId && !category.IsDeleted, asNoTracking: false, cancellationToken: cancellationToken);
            if (category is null)
                return Result<bool>.NotFound("Category not found.");

            var activeProduct = await _unitOfWork.GetRepository<Product>().FindAsync(product => product.CategoryId == categoryId && !product.IsDeleted, cancellationToken: cancellationToken);
            if (activeProduct is not null)
                return Result<bool>.Failure("Cannot delete category because it contains active products.");

            category.Deactivate();
            categoryRepository.Remove(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}
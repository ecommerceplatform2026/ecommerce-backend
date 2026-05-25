using Application.Common.Response;
using Application.DTOs.Product.ProductVariants;

namespace Application.Interfaces.Services
{
    public interface IProductVariantService
    {
        Task<Result<List<ProductVariantResponse>>> GetVariantsByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
        Task<Result<ProductVariantResponse>> GetVariantByIdAsync(Guid productId, Guid variantId, CancellationToken cancellationToken = default);
        Task<Result<ProductVariantResponse>> AddVariantAsync(Guid productId, CreateProductVariantRequest request, CancellationToken cancellationToken = default);
        Task<Result<ProductVariantResponse>> UpdateVariantAsync(Guid productId, Guid variantId, UpdateProductVariantRequest request, CancellationToken cancellationToken = default);
        Task<Result<bool>> UpdateStockAsync(Guid productId, Guid variantId, long newStock, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteVariantAsync(Guid productId, Guid variantId, CancellationToken cancellationToken = default);
    }
}

using Application.Common.Response;
using Application.DTOs.Product;

namespace Application.Interfaces.Services
{
    public interface IProductService
    {
        Task<Result<ProductResponse>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result<ProductDetailResponse>> GetProductDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result<PagedResult<ProductResponse>>> GetProductsAsync(ProductListingRequest request, CancellationToken cancellationToken = default);
        Task<Result<List<ProductResponse>>> GetAllProductsAsync(CancellationToken cancellationToken = default);
        Task<Result<ProductResponse>> CreateProductAsync(CreateProductRequest createProductRequest, CancellationToken cancellationToken = default);
        Task<Result<ProductResponse>> UpdateProductAsync(Guid id, UpdateProductRequest updateProductRequest, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
    }
}

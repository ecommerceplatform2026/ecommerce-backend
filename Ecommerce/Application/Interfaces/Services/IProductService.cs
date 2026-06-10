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
        Task<Result<ProductImageResponse>> UploadProductImageAsync(Guid productId, Stream imageStream, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteProductImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default);
        Task<Result<List<ProductImageResponse>>> GetProductImagesAsync(Guid productId, CancellationToken cancellationToken = default);
    }
}

using Application.DTOs.Product;

namespace Application.Interfaces.Services
{
    public interface IProductImageStorage
    {
        Task<ProductImageUploadResult> UploadAsync(
            Stream imageStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(string publicId, CancellationToken cancellationToken = default);
    }
}

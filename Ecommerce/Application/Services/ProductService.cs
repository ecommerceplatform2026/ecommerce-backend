using Application.Common.Caching;
using Application.Common.Response;
using Application.DTOs.Product;
using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Application.Mappings;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public sealed class ProductService : IProductService
    {
        private const long MaxImageSize = 10 * 1024 * 1024;
        private const string LikeEscape = "\\";
        private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/gif"
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductImageStorage _productImageStorage;
        private readonly ICacheService _cacheService;
        private readonly IProductRepository _productRepository;

        public ProductService(
            IUnitOfWork unitOfWork,
            IProductImageStorage productImageStorage,
            ICacheService cacheService,
            IProductRepository productRepository)
        {
            _unitOfWork = unitOfWork;
            _productImageStorage = productImageStorage;
            _cacheService = cacheService;
            _productRepository = productRepository;
        }

        public async Task<Result<ProductResponse>> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var cacheKey = CacheKeys.GetProductDetailKey(id);
            var response = await _cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var product = await _unitOfWork.GetRepository<Product>().FindAsync(
                        p => p.Id == id && !p.IsDeleted,
                        true,
                        cancellationToken,
                        x => x.Category,
                        x => x.ProductVariants);

                    return product?.ToProductResponse();
                },
                TimeSpan.FromHours(1),
                cancellationToken);

            if (response == null)
                return Result<ProductResponse>.NotFound("Product not found.");

            return Result<ProductResponse>.Success(response);
        }

        public async Task<Result<ProductDetailResponse>> GetProductDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var product = await _unitOfWork.GetRepository<Product>().FindAsync(
                p =>
                    p.Id == id &&
                    !p.IsDeleted &&
                    p.Status == ProductStatus.Active &&
                    !p.Category.IsDeleted &&
                    p.Category.Status == CategoryStatus.Active,
                includes: new System.Linq.Expressions.Expression<Func<Product, object>>[]
                {
                    x => x.Category,
                    x => x.ProductImages,
                    x => x.ProductVariants,
                    x => x.Reviews
                },
                cancellationToken: cancellationToken);

            if (product == null)
                return Result<ProductDetailResponse>.NotFound("Product not found.");

            return Result<ProductDetailResponse>.Success(product.ToProductDetailResponse());
        }

        public async Task<Result<PagedResult<ProductResponse>>> GetProductsAsync(ProductListingRequest request, CancellationToken cancellationToken = default)
        {
            if (request.MinPrice < 0 || request.MaxPrice < 0)
                return Result<PagedResult<ProductResponse>>.Failure("Price range cannot contain negative values.");

            if (request.MinPrice.HasValue && request.MaxPrice.HasValue && request.MinPrice > request.MaxPrice)
                return Result<PagedResult<ProductResponse>>.Failure("Min price cannot be greater than max price.");

            var pagedResult = await _productRepository.GetProductsAsync(request, cancellationToken);

            var result = new PagedResult<ProductResponse>
            {
                Items = pagedResult.Items.Select(product => product.ToProductResponse()).ToList(),
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalCount = pagedResult.TotalCount
            };

            return Result<PagedResult<ProductResponse>>.Success(result);
        }

        public async Task<Result<List<ProductResponse>>> GetAllProductsAsync(CancellationToken cancellationToken = default)
        {
            var response = await _cacheService.GetOrAddAsync(
                CacheKeys.ProductsAll,
                async () =>
                {
                    var products = await _unitOfWork.GetRepository<Product>().GetAllAsync(
                        p => !p.IsDeleted,
                        cancellationToken,
                        x => x.Category,
                        x => x.ProductVariants,
                        x => x.ProductImages,
                        x => x.Reviews);

                    return products.Select(p => p.ToProductResponse()).ToList();
                },
                TimeSpan.FromHours(1),
                cancellationToken);

            return Result<List<ProductResponse>>.Success(response ?? new List<ProductResponse>());
        }

        public async Task<Result<ProductResponse>> CreateProductAsync(CreateProductRequest createProductRequest, CancellationToken cancellationToken = default)
        {
            var category = await _unitOfWork.GetRepository<Category>().FindAsync(c => c.Id == createProductRequest.CategoryId && !c.IsDeleted, cancellationToken: cancellationToken);
            if (category == null)
                return Result<ProductResponse>.NotFound("Category not found.");

            var product = createProductRequest.ToEntity();

            await _unitOfWork.GetRepository<Product>().AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cacheService.RemoveAsync(CacheKeys.ProductsAll, cancellationToken);

            return await GetProductByIdAsync(product.Id, cancellationToken);
        }

        public async Task<Result<ProductResponse>> UpdateProductAsync(Guid id, UpdateProductRequest updateProductRequest, CancellationToken cancellationToken = default)
        {
            var productRepository = _unitOfWork.GetRepository<Product>();
            var product = await productRepository.FindAsync(p => p.Id == id && !p.IsDeleted, asNoTracking: false, cancellationToken: cancellationToken);

            if (product == null)
                return Result<ProductResponse>.NotFound("Product not found.");

            if (product.CategoryId != updateProductRequest.CategoryId)
            {
                var category = await _unitOfWork.GetRepository<Category>().FindAsync(c => c.Id == updateProductRequest.CategoryId && !c.IsDeleted, cancellationToken: cancellationToken);
                if (category == null)
                    return Result<ProductResponse>.NotFound("Category not found.");
            }

            updateProductRequest.MapToEntity(product);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cacheService.RemoveAsync(CacheKeys.ProductsAll, cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetProductDetailKey(id), cancellationToken);

            return await GetProductByIdAsync(id, cancellationToken);
        }

        public async Task<Result<bool>> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var productRepository = _unitOfWork.GetRepository<Product>();
            var product = await productRepository.FindAsync(p => p.Id == id && !p.IsDeleted, asNoTracking: false, cancellationToken: cancellationToken);

            if (product == null)
                return Result<bool>.NotFound("Product not found.");

            product.Deactivate();
            productRepository.Remove(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cacheService.RemoveAsync(CacheKeys.ProductsAll, cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetProductDetailKey(id), cancellationToken);

            return Result<bool>.Success(true);
        }

        public async Task<Result<ProductImageResponse>> UploadProductImageAsync(
            Guid productId,
            Stream imageStream,
            string fileName,
            string contentType,
            long fileSize,
            CancellationToken cancellationToken = default)
        {
            var validationError = ValidateImage(fileName, contentType, fileSize);
            if (validationError is not null)
                return Result<ProductImageResponse>.Failure(validationError);

            var productExists = await _unitOfWork.GetRepository<Product>()
                .TotalAsync(product => product.Id == productId && !product.IsDeleted) > 0;

            if (!productExists)
                return Result<ProductImageResponse>.NotFound("Product not found.");

            ProductImageUploadResult uploadedImage;
            try
            {
                uploadedImage = await _productImageStorage.UploadAsync(imageStream, fileName, contentType, cancellationToken);
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                return Result<ProductImageResponse>.Failure(ex.Message);
            }

            var productImage = new ProductImage
            {
                ProductId = productId,
                ImageUrl = uploadedImage.ImageUrl,
                CloudinaryPublicId = uploadedImage.PublicId
            };

            try
            {
                await _unitOfWork.GetRepository<ProductImage>().AddAsync(productImage, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await TryDeleteImageAsync(uploadedImage.PublicId, cancellationToken);
                throw;
            }

            return Result<ProductImageResponse>.Success(new ProductImageResponse
            {
                Id = productImage.Id,
                ImageUrl = productImage.ImageUrl
            });
        }

        public async Task<Result<bool>> DeleteProductImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default)
        {
            var imageRepository = _unitOfWork.GetRepository<ProductImage>();
            var productImage = await imageRepository.FindAsync(
                image => image.Id == imageId && image.ProductId == productId && !image.IsDeleted,
                asNoTracking: false,
                cancellationToken: cancellationToken);

            if (productImage == null)
                return Result<bool>.NotFound("Product image not found.");

            var publicId = productImage.CloudinaryPublicId;

            imageRepository.Remove(productImage);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(publicId))
                await TryDeleteImageAsync(publicId, cancellationToken);

            return Result<bool>.Success(true);
        }



        private static string? ValidateImage(string fileName, string contentType, long fileSize)
        {
            if (fileSize <= 0)
                return "Image file is required.";

            if (fileSize > MaxImageSize)
                return "Image file size cannot exceed 10 MB.";

            if (string.IsNullOrWhiteSpace(fileName))
                return "Image file name is required.";

            if (string.IsNullOrWhiteSpace(contentType) || !AllowedImageContentTypes.Contains(contentType))
                return "Only JPEG, PNG, WEBP, and GIF images are allowed.";

            return null;
        }

        private async Task TryDeleteImageAsync(string publicId, CancellationToken cancellationToken)
        {
            try
            {
                await _productImageStorage.DeleteAsync(publicId, cancellationToken);
            }
            catch
            {
                // The database failure is the caller-visible error; cleanup is best effort.
            }
        }
    }
}

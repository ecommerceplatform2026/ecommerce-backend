using Application.DTOs.Product;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/products")]
    public sealed class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _productService.GetProductByIdAsync(id, cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet("{id:guid}/detail")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductDetailById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _productService.GetProductDetailByIdAsync(id, cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
        {
            var result = await _productService.GetAllProductsAsync(cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchProducts([FromQuery] ProductListingRequest request, CancellationToken cancellationToken)
        {
            var result = await _productService.GetProductsAsync(request, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
        {
            var result = await _productService.CreateProductAsync(request, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
        {
            var result = await _productService.UpdateProductAsync(id, request, cancellationToken);
            return this.FromResult(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
        {
            var result = await _productService.DeleteProductAsync(id, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPost("{id:guid}/images")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProductImage(Guid id, IFormFile? image, CancellationToken cancellationToken)
        {
            if (image == null)
                return BadRequest(new Common.Responses.ApiResponse<object>
                {
                    Success = false,
                    Errors = new List<string> { "Image file is required." }
                });

            await using var stream = image.OpenReadStream();
            var result = await _productService.UploadProductImageAsync(
                id,
                stream,
                image.FileName,
                image.ContentType,
                image.Length,
                cancellationToken);

            return this.FromResult(result);
        }

        [HttpDelete("{id:guid}/images/{imageId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProductImage(Guid id, Guid imageId, CancellationToken cancellationToken)
        {
            var result = await _productService.DeleteProductImageAsync(id, imageId, cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet("{productId:guid}/images")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductImages(Guid productId, CancellationToken cancellationToken)
        {
            var result = await _productService.GetProductImagesAsync(productId, cancellationToken);
            return this.FromResult(result);
        }
    }
}

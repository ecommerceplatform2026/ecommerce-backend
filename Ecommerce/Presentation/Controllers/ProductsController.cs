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
            var result = await _productService.GetProductDetailByIdAsync(id, cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProducts([FromQuery] ProductListingRequest request, CancellationToken cancellationToken)
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
    }
}

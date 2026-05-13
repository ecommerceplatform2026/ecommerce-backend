using Application.DTOs.Product.ProductVariant;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/products/{productId:guid}/variants")]
    public sealed class ProductVariantsController : ControllerBase
    {
        private readonly IProductVariantService _variantService;

        public ProductVariantsController(IProductVariantService variantService)
        {
            _variantService = variantService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetVariants(Guid productId, CancellationToken cancellationToken)
        {
            var result = await _variantService.GetVariantsByProductIdAsync(productId, cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet("{variantId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVariantById(Guid productId, Guid variantId, CancellationToken cancellationToken)
        {
            var result = await _variantService.GetVariantByIdAsync(productId, variantId, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddVariant(Guid productId, [FromBody] CreateProductVariantRequest request, CancellationToken cancellationToken)
        {
            var result = await _variantService.AddVariantAsync(productId, request, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPut("{variantId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateVariant(Guid productId, Guid variantId, [FromBody] UpdateProductVariantRequest request, CancellationToken cancellationToken)
        {
            var result = await _variantService.UpdateVariantAsync(productId, variantId, request, cancellationToken);
            return this.FromResult(result);
        }

        [HttpDelete("{variantId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVariant(Guid productId, Guid variantId, CancellationToken cancellationToken)
        {
            var result = await _variantService.DeleteVariantAsync(productId, variantId, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPatch("{variantId:guid}/stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStock(Guid productId, Guid variantId, [FromBody] long newStock, CancellationToken cancellationToken)
        {
            var result = await _variantService.UpdateStockAsync(productId, variantId, newStock, cancellationToken);
            return this.FromResult(result);
        }
    }
}

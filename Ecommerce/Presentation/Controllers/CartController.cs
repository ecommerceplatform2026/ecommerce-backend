using Application.DTOs.Cart;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;

namespace Presentation.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/cart")]
    public sealed class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
        }

        [HttpGet]
        public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
        {
            var result = await _cartService.GetCartAsync(cancellationToken);
            return this.FromResult(result);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request, CancellationToken cancellationToken)
        {
            var result = await _cartService.AddToCartAsync(request, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPut("items/{variantId:guid}")]
        public async Task<IActionResult> UpdateCartItem(Guid variantId, [FromBody] UpdateCartItemRequest request, CancellationToken cancellationToken)
        {
            var result = await _cartService.UpdateCartItemAsync(variantId, request, cancellationToken);
            return this.FromResult(result);
        }

        [HttpDelete("items/{variantId:guid}")]
        public async Task<IActionResult> RemoveCartItem(Guid variantId, CancellationToken cancellationToken)
        {
            var result = await _cartService.RemoveCartItemAsync(variantId, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPost("merge")]
        public async Task<IActionResult> MergeCart([FromBody] MergeCartRequest request, CancellationToken cancellationToken)
        {
            var result = await _cartService.MergeCartAsync(request, cancellationToken);
            return this.FromResult(result);
        }
    }
}

using Application.DTOs.Wishlist;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Presentation.Controllers
{
    [ApiController]
    [Route("api/wishlist")]
    public sealed class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService ?? throw new ArgumentNullException(nameof(wishlistService));
        }

        [HttpGet]
        public async Task<IActionResult> GetWishlist([FromQuery] List<Guid>? guestVariantIds, CancellationToken cancellationToken)
        {
            var result = await _wishlistService.GetWishlistAsync(guestVariantIds, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistRequest request, CancellationToken cancellationToken)
        {
            var result = await _wishlistService.AddToWishlistAsync(request, cancellationToken);
            return this.FromResult(result);
        }

        [HttpDelete("items/{variantId:guid}")]
        public async Task<IActionResult> RemoveFromWishlist(Guid variantId, CancellationToken cancellationToken)
        {
            var result = await _wishlistService.RemoveFromWishlistAsync(variantId, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPost("merge")]
        [Authorize]
        public async Task<IActionResult> MergeWishlist([FromBody] MergeWishlistRequest request, CancellationToken cancellationToken)
        {
            var result = await _wishlistService.MergeWishlistAsync(request, cancellationToken);
            return this.FromResult(result);
        }
    }
}

using Application.DTOs.Review;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;

namespace Presentation.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/reviews")]
    public sealed class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
        }

        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
        {
            var result = await _reviewService.CreateReviewAsync(request, cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet("/api/products/{productId:guid}/reviews")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductReviews(Guid productId, [FromQuery] GetProductReviewsRequest request, CancellationToken cancellationToken)
        {
            var result = await _reviewService.GetProductReviewsAsync(productId, request, cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet("eligibility")]
        public async Task<IActionResult> CheckReviewEligibility([FromQuery] Guid productId, CancellationToken cancellationToken)
        {
            var result = await _reviewService.GetReviewEligibilityAsync(productId, cancellationToken);
            return this.FromResult(result);
        }
    }
}

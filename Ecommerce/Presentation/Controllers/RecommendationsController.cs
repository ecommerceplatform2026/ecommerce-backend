using Application.DTOs.Product;
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
    [Route("api/recommendations")]
    public sealed class RecommendationsController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationsController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
        }

        [HttpGet("popular")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPopularProducts(CancellationToken cancellationToken)
        {
            var result = await _recommendationService.GetPopularProductsAsync(cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet("for-you")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPersonalizedRecommendations(CancellationToken cancellationToken)
        {
            var result = await _recommendationService.GetPersonalizedRecommendationsAsync(cancellationToken);
            return this.FromResult(result);
        }

        [HttpGet("similar/{productId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSimilarProducts(Guid productId, CancellationToken cancellationToken)
        {
            var result = await _recommendationService.GetSimilarProductsAsync(productId, cancellationToken);
            return this.FromResult(result);
        }
    }
}

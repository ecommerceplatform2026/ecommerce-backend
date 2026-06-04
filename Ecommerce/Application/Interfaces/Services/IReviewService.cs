using Application.Common.Response;
using Application.DTOs.Review;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IReviewService
    {
        Task<Result<ReviewResponse>> CreateReviewAsync(CreateReviewRequest request, CancellationToken cancellationToken = default);
        Task<Result<PagedResult<ReviewResponse>>> GetProductReviewsAsync(Guid productId, GetProductReviewsRequest request, CancellationToken cancellationToken = default);
        Task<Result<ReviewEligibilityResponse>> GetReviewEligibilityAsync(Guid productId, CancellationToken cancellationToken = default);
    }
}

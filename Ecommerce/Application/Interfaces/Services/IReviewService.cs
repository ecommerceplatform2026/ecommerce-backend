using Application.Common.Response;
using Application.DTOs.Review;

namespace Application.Interfaces.Services
{
    public interface IReviewService
    {
        Task<Result<ReviewResponse>> CreateReviewAsync(CreateReviewRequest request, CancellationToken cancellationToken = default);
    }
}

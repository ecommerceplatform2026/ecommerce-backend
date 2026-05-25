using Application.DTOs.Review;
using Domain.Entities;

namespace Application.Mappings
{
    public static class ReviewMappingExtensions
    {
        public static ReviewResponse ToReviewResponse(this Review review)
        {
            return new ReviewResponse(
                review.Id,
                review.UserId,
                review.ProductId,
                review.OrderId,
                review.Rating,
                review.Title,
                review.Comment,
                review.Status,
                review.CreatedAt);
        }
    }
}

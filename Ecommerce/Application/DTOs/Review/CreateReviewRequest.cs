using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Review
{
    public sealed record CreateReviewRequest(
        Guid ProductId,
        Guid OrderId,
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")] int Rating,
        string? Title,
        string? Comment);
}

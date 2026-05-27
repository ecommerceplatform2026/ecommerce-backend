using Application.Common.Validations;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Review
{
    public sealed record CreateReviewRequest(
        [NotEmptyGuid(ErrorMessage = "ProductId is required.")] Guid ProductId,
        [NotEmptyGuid(ErrorMessage = "OrderId is required.")] Guid OrderId,
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")] int Rating,
        [MaxLength(200, ErrorMessage = "Title must not exceed 200 characters.")] string? Title,
        string? Comment);
}

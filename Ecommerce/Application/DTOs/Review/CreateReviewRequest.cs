using Application.Common.Validations;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Review
{
    public sealed record CreateReviewRequest(
        [property: NotEmptyGuid(ErrorMessage = "ProductId is required.")] Guid ProductId,
        [property: NotEmptyGuid(ErrorMessage = "OrderId is required.")] Guid OrderId,
        [property: Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")] int Rating,
        [property: MaxLength(200, ErrorMessage = "Title must not exceed 200 characters.")] string? Title,
        string? Comment);
}

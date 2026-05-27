using Domain.Enums;

namespace Application.DTOs.Review
{
    public sealed record ReviewResponse(
        Guid Id,
        Guid UserId,
        Guid ProductId,
        Guid OrderId,
        int Rating,
        string? Title,
        string? Comment,
        ReviewStatus Status,
        DateTime CreatedAt);
}

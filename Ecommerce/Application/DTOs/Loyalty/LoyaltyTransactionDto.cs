using Domain.Enums;

namespace Application.DTOs.Loyalty
{
    public sealed record LoyaltyTransactionDto(
        Guid Id,
        DateTime Date,
        string Type,
        int Points,
        string? OrderId,
        string? Description);
}

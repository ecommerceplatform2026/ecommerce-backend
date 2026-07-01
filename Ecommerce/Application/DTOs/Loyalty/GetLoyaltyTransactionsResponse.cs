using Domain.Enums;

namespace Application.DTOs.Loyalty
{
    public sealed record GetLoyaltyTransactionResponse(
        Guid Id,
        DateTime Date,
        LoyaltyTransactionType Type,
        int Points,
        LoyaltyTransactionStatus Status,
        string? OrderId,
        string? Description);
}

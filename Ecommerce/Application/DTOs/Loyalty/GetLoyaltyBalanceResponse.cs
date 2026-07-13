namespace Application.DTOs.Loyalty
{
    public sealed record GetLoyaltyBalanceResponse(
        int Balance,
        int PendingPoints,
        int TotalEarned,
        int TotalRedeemed,
        long DiscountEquivalent,
        DateTime LastUpdated);
}

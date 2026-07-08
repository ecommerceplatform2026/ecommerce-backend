namespace Application.DTOs.Loyalty
{
    public sealed record GetLoyaltyBalanceResponse(
        int Balance,
        long DiscountEquivalent,
        DateTime LastUpdated);
}

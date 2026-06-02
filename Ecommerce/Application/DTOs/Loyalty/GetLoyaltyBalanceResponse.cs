namespace Application.DTOs.Loyalty
{
    public sealed record GetLoyaltyBalanceResponse(
        int Balance,
        int DiscountEquivalent,
        DateTime LastUpdated);
}

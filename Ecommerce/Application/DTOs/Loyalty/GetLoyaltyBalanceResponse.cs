namespace Application.DTOs.Loyalty
{
    public sealed record GetLoyaltyBalanceResponse(
        int Balance,
        int VndEquivalent,
        DateTime LastUpdated);
}

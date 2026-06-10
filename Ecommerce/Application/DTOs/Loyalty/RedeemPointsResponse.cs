namespace Application.DTOs.Loyalty
{
    public sealed record RedeemPointsResponse(
        int RedeemedPoints,
        long DiscountAmount,
        int RemainingBalance);
}

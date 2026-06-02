namespace Application.DTOs.Loyalty
{
    public sealed record GetLoyaltyTransactionsResponse(
        List<LoyaltyTransactionDto> Transactions,
        int TotalCount,
        int PageNumber,
        int PageSize);
}

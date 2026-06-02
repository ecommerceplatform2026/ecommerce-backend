using Application.DTOs.Loyalty;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mappings
{
    public static class LoyaltyMappingExtensions
    {
        public static GetLoyaltyBalanceResponse ToGetLoyaltyBalanceResponse(this LoyaltyAccount account)
        {
            const int VndPerPoint = 10_000;
            
            var totalBalance = account.AvailablePoints + account.PendingPoints;
            
            // Handle negative balance edge case
            if (totalBalance < 0)
            {
                totalBalance = 0;
            }

            var vndEquivalent = totalBalance * VndPerPoint / 100;

            return new GetLoyaltyBalanceResponse(
                Balance: totalBalance,
                VndEquivalent: vndEquivalent,
                LastUpdated: DateTime.UtcNow);
        }

        public static LoyaltyTransactionDto ToLoyaltyTransactionDto(this LoyaltyTransaction transaction)
        {
            var displayPoints = transaction.Type == LoyaltyTransactionType.Earn 
                ? transaction.Points 
                : -transaction.Points;

            return new LoyaltyTransactionDto(
                Id: transaction.Id,
                Date: transaction.CreatedAt,
                Type: transaction.Type.ToString(),
                Points: displayPoints,
                OrderId: transaction.OrderId?.ToString(),
                Description: transaction.Description);
        }
    }
}

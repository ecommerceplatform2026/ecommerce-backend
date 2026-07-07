using Application.DTOs.Loyalty;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mappings
{
    public static class LoyaltyMappingExtensions
    {
        public static GetLoyaltyBalanceResponse ToGetLoyaltyBalanceResponse(this LoyaltyAccount account)
        {
            var totalBalance = account.AvailablePoints + account.PendingPoints;
            
            // Handle negative balance edge case
            if (totalBalance < 0)
            {
                totalBalance = 0;
            }

            var vndEquivalent = (long)(totalBalance * ILoyaltyService.PointRedeemRate);

            return new GetLoyaltyBalanceResponse(
                Balance: totalBalance,
                DiscountEquivalent: vndEquivalent,
                LastUpdated: DateTime.UtcNow);
        }

        public static GetLoyaltyTransactionResponse ToLoyaltyTransactionResponse(this LoyaltyTransaction transaction)
        {
            var displayPoints = transaction.Type == LoyaltyTransactionType.Earn
                ? transaction.Points
                : -transaction.Points;

            return new GetLoyaltyTransactionResponse(
                Id: transaction.Id,
                Date: transaction.CreatedAt,
                Type: transaction.Type,
                Points: displayPoints,
                Status: transaction.Status,
                OrderId: transaction.OrderId?.ToString(),
                Description: transaction.Description);
        }
    }
}

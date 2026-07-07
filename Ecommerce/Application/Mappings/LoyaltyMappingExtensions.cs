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
            var balance = account.AvailablePoints;
            var vndEquivalent = (long)(balance * ILoyaltyService.PointRedeemRate);

            return new GetLoyaltyBalanceResponse(
                Balance: balance,
                PendingPoints: account.PendingPoints,
                TotalEarned: account.TotalEarn,
                TotalRedeemed: account.TotalRedeem,
                DiscountEquivalent: vndEquivalent,
                LastUpdated: DateTime.UtcNow);
        }

        public static GetLoyaltyTransactionResponse ToLoyaltyTransactionResponse(this LoyaltyTransaction transaction)
        {
            return new GetLoyaltyTransactionResponse(
                Id: transaction.Id,
                Date: transaction.CreatedAt,
                Type: transaction.Type,
                Points: transaction.Points,
                Status: transaction.Status,
                OrderId: transaction.OrderId?.ToString(),
                Description: transaction.Description);
        }
    }
}

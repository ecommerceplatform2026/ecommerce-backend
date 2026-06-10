using Domain.Common;
using Domain.Enums;
using System;

namespace Domain.Entities
{
    public class LoyaltyTransaction : BaseEntity
    {
        public Guid LoyaltyAccountId { get; private set; }
        public Guid? OrderId { get; private set; }
        public int Points { get; private set; }
        public LoyaltyTransactionType Type { get; private set; }
        public LoyaltyTransactionStatus Status { get; private set; }
        public string? Description { get; private set; }

        public LoyaltyAccount? LoyaltyAccount { get; set; }
        public Order? Order { get; set; }

        private LoyaltyTransaction() { }

        private LoyaltyTransaction(
            Guid loyaltyAccountId,
            Guid? orderId,
            int points,
            LoyaltyTransactionType type,
            LoyaltyTransactionStatus status,
            string? description)
        {
            if (loyaltyAccountId == Guid.Empty)
                throw new ArgumentException("Loyalty account ID cannot be empty.", nameof(loyaltyAccountId));
            if (points <= 0)
                throw new ArgumentException("Points must be greater than zero.", nameof(points));

            LoyaltyAccountId = loyaltyAccountId;
            OrderId = orderId;
            Points = points;
            Type = type;
            Status = status;
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        }

        public static LoyaltyTransaction CreatePendingEarn(Guid loyaltyAccountId, Guid orderId, int points)
        {
            if (orderId == Guid.Empty)
                throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));

            return new LoyaltyTransaction(
                loyaltyAccountId,
                orderId,
                points,
                LoyaltyTransactionType.Earn,
                LoyaltyTransactionStatus.Pending,
                "Points earned from delivered order.");
        }

        public static LoyaltyTransaction CreatePendingRedeem(Guid loyaltyAccountId, Guid orderId, int points)
        {
            if (orderId == Guid.Empty)
                throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));

            return new LoyaltyTransaction(
                loyaltyAccountId,
                orderId,
                points,
                LoyaltyTransactionType.Redeem,
                LoyaltyTransactionStatus.Pending,
                "Points redeemed from order.");
        }

        public void Complete()
        {
            if (Status == LoyaltyTransactionStatus.Completed)
            {
                return;
            }

            if (Status == LoyaltyTransactionStatus.Cancelled)
            {
                throw new InvalidOperationException("Cannot complete a cancelled loyalty transaction.");
            }

            Status = LoyaltyTransactionStatus.Completed;
        }
    }
}

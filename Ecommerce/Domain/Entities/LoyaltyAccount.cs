using Domain.Common;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities
{
    public class LoyaltyAccount : BaseEntity
    {
        public Guid UserId { get; private set; }
        public int AvailablePoints { get; private set; }
        public int PendingPoints { get; private set; }
        public int TotalEarn { get; private set; }
        public int TotalRedeem { get; private set; }

        public User? User { get; set; }
        public virtual ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();

        private LoyaltyAccount() { }

        private LoyaltyAccount(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            UserId = userId;
            AvailablePoints = 0;
            PendingPoints = 0;
            TotalEarn = 0;
            TotalRedeem = 0;
        }

        public static LoyaltyAccount Create(Guid userId)
        {
            return new LoyaltyAccount(userId);
        }

        public void AddPendingPoints(int points)
        {
            if (points <= 0)
                throw new ArgumentException("Points must be greater than zero.", nameof(points));

            PendingPoints += points;
        }

        public void CompletePendingPoints(int points)
        {
            if (points <= 0)
                throw new ArgumentException("Points must be greater than zero.", nameof(points));

            if (PendingPoints < points)
                throw new InvalidOperationException("Not enough pending points to complete.");

            PendingPoints -= points;
            AvailablePoints += points;
        }

        public void DeductAvailablePoints(int points)
        {
            if (points <= 0)
                throw new ArgumentException("Points must be greater than zero.", nameof(points));

            if (AvailablePoints < points)
                throw new InvalidOperationException("Insufficient available points.");

            AvailablePoints -= points;
        }

        public void AddAvailablePoints(int points)
        {
            if (points <= 0)
                throw new ArgumentException("Points must be greater than zero.", nameof(points));

            AvailablePoints += points;
        }

        public void ExpirePoints(int points)
        {
            if (points <= 0)
                throw new ArgumentException("Points must be greater than zero.", nameof(points));
            if (AvailablePoints < points)
                throw new InvalidOperationException("Not enough available points to expire.");

            AvailablePoints -= points;
        }

        public void DeductPendingPoints(int points)
        {
            if (points <= 0)
                throw new ArgumentException("Points must be greater than zero.", nameof(points));

            if (PendingPoints < points)
                throw new InvalidOperationException("Not enough pending points.");

            PendingPoints -= points;
        }

        public void ReverseEarnedPoints(int points)
        {
            if (points <= 0)
                throw new ArgumentException("Points must be greater than zero.", nameof(points));

            if (AvailablePoints < points)
                throw new InvalidOperationException("Insufficient available points to reverse.");

            AvailablePoints -= points;
        }

        public void RecalculateTotals(IEnumerable<LoyaltyTransaction>? transactions)
        {
            var transactionList = transactions ?? Enumerable.Empty<LoyaltyTransaction>();

            TotalEarn = transactionList
                .Where(t => t.Type == LoyaltyTransactionType.Earn && t.Status == LoyaltyTransactionStatus.Completed)
                .Sum(t => t.Points);

            TotalRedeem = transactionList
                .Where(t => t.Type == LoyaltyTransactionType.Redeem && t.Status == LoyaltyTransactionStatus.Completed)
                .Sum(t => t.Points);
        }
    }
}

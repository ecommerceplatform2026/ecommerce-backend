using Domain.Common;
using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class LoyaltyAccount : BaseEntity
    {
        public Guid UserId { get; private set; }
        public int AvailablePoints { get; private set; }
        public int PendingPoints { get; private set; }

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

        public void ReverseEarnedPoints(int points)
        {
            if (points <= 0)
                throw new ArgumentException("Points must be greater than zero.", nameof(points));

            if (AvailablePoints < points)
                throw new InvalidOperationException("Insufficient available points to reverse.");

            AvailablePoints -= points;
        }
    }
}

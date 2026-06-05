using Domain.Entities;
using FluentAssertions;
using System;

namespace Ecommerce.UnitTests.EntityTests
{
    public class LoyaltyAccountTests
    {
        [Fact]
        public void DeductAvailablePoints_ValidPoints_DeductsBalance()
        {
            var account = LoyaltyAccount.Create(Guid.NewGuid());
            account.AddAvailablePoints(500);

            account.DeductAvailablePoints(300);

            account.AvailablePoints.Should().Be(200);
        }

        [Fact]
        public void DeductAvailablePoints_InsufficientBalance_Throws()
        {
            var account = LoyaltyAccount.Create(Guid.NewGuid());
            account.AddAvailablePoints(100);

            Action act = () => account.DeductAvailablePoints(500);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Insufficient available points.");
        }

        [Fact]
        public void DeductAvailablePoints_ZeroOrNegativePoints_Throws()
        {
            var account = LoyaltyAccount.Create(Guid.NewGuid());
            account.AddAvailablePoints(500);

            Action act = () => account.DeductAvailablePoints(0);

            act.Should().Throw<ArgumentException>()
                .WithMessage("Points must be greater than zero.*");
        }

        [Fact]
        public void AddAvailablePoints_ValidPoints_AddsToBalance()
        {
            var account = LoyaltyAccount.Create(Guid.NewGuid());

            account.AddAvailablePoints(300);

            account.AvailablePoints.Should().Be(300);
        }
        [Fact]
        public void ReverseEarnedPoints_Valid_DeductsBalance()
        {
            var account = LoyaltyAccount.Create(Guid.NewGuid());
            account.AddAvailablePoints(500);

            account.ReverseEarnedPoints(300);

            account.AvailablePoints.Should().Be(200);
        }

        [Fact]
        public void ReverseEarnedPoints_InsufficientBalance_Throws()
        {
            var account = LoyaltyAccount.Create(Guid.NewGuid());
            account.AddAvailablePoints(100);

            Action act = () => account.ReverseEarnedPoints(500);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Insufficient available points to reverse.");
        }

        [Fact]
        public void ReverseEarnedPoints_ZeroOrNegativePoints_Throws()
        {
            var account = LoyaltyAccount.Create(Guid.NewGuid());

            Action act = () => account.ReverseEarnedPoints(0);

            act.Should().Throw<ArgumentException>()
                .WithMessage("Points must be greater than zero.*");
        }
    }
}

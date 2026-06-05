using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using System;
using System.Reflection;

namespace Ecommerce.UnitTests.EntityTests
{
    public class LoyaltyTransactionTests
    {
        [Fact]
        public void CreatePendingRedeem_CreatesCorrectTransaction()
        {
            var accountId = Guid.NewGuid();
            var orderId = Guid.NewGuid();

            var transaction = LoyaltyTransaction.CreatePendingRedeem(accountId, orderId, 300);

            transaction.LoyaltyAccountId.Should().Be(accountId);
            transaction.OrderId.Should().Be(orderId);
            transaction.Points.Should().Be(300);
            transaction.Type.Should().Be(LoyaltyTransactionType.Redeem);
            transaction.Status.Should().Be(LoyaltyTransactionStatus.Pending);
            transaction.Description.Should().Be("Points redeemed from order.");
        }

        [Fact]
        public void CreatePendingRedeem_EmptyOrderId_Throws()
        {
            Action act = () => LoyaltyTransaction.CreatePendingRedeem(Guid.NewGuid(), Guid.Empty, 300);

            act.Should().Throw<ArgumentException>()
                .WithMessage("Order ID cannot be empty.*");
        }

        [Fact]
        public void Complete_OnPending_SetsCompleted()
        {
            var transaction = LoyaltyTransaction.CreatePendingRedeem(Guid.NewGuid(), Guid.NewGuid(), 300);

            transaction.Complete();

            transaction.Status.Should().Be(LoyaltyTransactionStatus.Completed);
        }

        [Fact]
        public void Complete_OnCompleted_NoOp()
        {
            var transaction = LoyaltyTransaction.CreatePendingRedeem(Guid.NewGuid(), Guid.NewGuid(), 300);
            transaction.Complete();

            transaction.Complete();

            transaction.Status.Should().Be(LoyaltyTransactionStatus.Completed);
        }

        [Fact]
        public void Complete_OnCancelled_Throws()
        {
            var transaction = LoyaltyTransaction.CreatePendingRedeem(Guid.NewGuid(), Guid.NewGuid(), 300);
            typeof(LoyaltyTransaction)
                .GetProperty(nameof(LoyaltyTransaction.Status), BindingFlags.Instance | BindingFlags.Public)!
                .SetValue(transaction, LoyaltyTransactionStatus.Cancelled);

            Action act = () => transaction.Complete();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot complete a cancelled loyalty transaction.");
        }
    }
}

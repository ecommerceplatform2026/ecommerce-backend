using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.UnitTests.Services
{
    public class LoyaltyServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IUniqueConstraintChecker> _uniqueConstraintCheckerMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IGenericRepository<Order>> _orderRepositoryMock;
        private readonly Mock<IGenericRepository<LoyaltyAccount>> _accountRepositoryMock;
        private readonly Mock<IGenericRepository<LoyaltyTransaction>> _transactionRepositoryMock;
        private readonly Mock<IGenericRepository<User>> _userRepositoryMock;
        private readonly LoyaltyService _service;

        public LoyaltyServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _uniqueConstraintCheckerMock = new Mock<IUniqueConstraintChecker>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _orderRepositoryMock = new Mock<IGenericRepository<Order>>();
            _accountRepositoryMock = new Mock<IGenericRepository<LoyaltyAccount>>();
            _transactionRepositoryMock = new Mock<IGenericRepository<LoyaltyTransaction>>();
            _userRepositoryMock = new Mock<IGenericRepository<User>>();

            _unitOfWorkMock
                .Setup(u => u.GetRepository<Order>())
                .Returns(_orderRepositoryMock.Object);
            _unitOfWorkMock
                .Setup(u => u.GetRepository<LoyaltyAccount>())
                .Returns(_accountRepositoryMock.Object);
            _unitOfWorkMock
                .Setup(u => u.GetRepository<LoyaltyTransaction>())
                .Returns(_transactionRepositoryMock.Object);
            _unitOfWorkMock
                .Setup(u => u.GetRepository<User>())
                .Returns(_userRepositoryMock.Object);

            _service = new LoyaltyService(_unitOfWorkMock.Object, _uniqueConstraintCheckerMock.Object, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task CreatePendingLoyaltyTransactionsAsync_WhenOrderIdIsEmpty_ReturnsFailure()
        {
            var result = await _service.CreatePendingLoyaltyTransactionsAsync(Guid.Empty, null, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Order ID cannot be empty.");
        }

        [Fact]
        public async Task CreatePendingLoyaltyTransactionsAsync_WhenOrderNotFound_ReturnsNotFound()
        {
            SetupOrder(null);

            var result = await _service.CreatePendingLoyaltyTransactionsAsync(Guid.NewGuid(), null, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Order not found.");
        }

        [Fact]
        public async Task CreatePendingLoyaltyTransactionsAsync_WhenFirstEarn_CreatesAccountAndPendingEarnTransaction()
        {
            var order = CreateOrderWithSubtotal(25_000, OrderStatus.Confirmed);
            LoyaltyAccount? addedAccount = null;
            LoyaltyTransaction? addedTransaction = null;

            SetupOrder(order);
            SetupAccount(null);
            _accountRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<LoyaltyAccount>(), It.IsAny<CancellationToken>()))
                .Callback<LoyaltyAccount, CancellationToken>((account, _) => addedAccount = account)
                .Returns(Task.CompletedTask);
            _transactionRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<LoyaltyTransaction>(), It.IsAny<CancellationToken>()))
                .Callback<LoyaltyTransaction, CancellationToken>((transaction, _) => addedTransaction = transaction)
                .Returns(Task.CompletedTask);
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _service.CreatePendingLoyaltyTransactionsAsync(order.Id, null, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(2);
            addedAccount.Should().NotBeNull();
            addedAccount!.UserId.Should().Be(order.UserId);
            addedAccount.PendingPoints.Should().Be(2);
            addedTransaction.Should().NotBeNull();
            addedTransaction!.Points.Should().Be(2);
            addedTransaction.Type.Should().Be(LoyaltyTransactionType.Earn);
            addedTransaction.Status.Should().Be(LoyaltyTransactionStatus.Pending);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreatePendingLoyaltyTransactionsAsync_WithRedemption_DeductsPointsAndCreatesRedeemTransaction()
        {
            var order = CreateOrderWithSubtotal(100_000, OrderStatus.Confirmed);
            var account = LoyaltyAccount.Create(order.UserId);
            account.AddAvailablePoints(500);
            LoyaltyTransaction? addedRedeem = null;
            LoyaltyTransaction? addedEarn = null;

            SetupOrder(order);
            SetupAccount(account);
            _transactionRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<LoyaltyTransaction>(), It.IsAny<CancellationToken>()))
                .Callback<LoyaltyTransaction, CancellationToken>((t, _) =>
                {
                    if (t.Type == LoyaltyTransactionType.Redeem) addedRedeem = t;
                    if (t.Type == LoyaltyTransactionType.Earn) addedEarn = t;
                })
                .Returns(Task.CompletedTask);
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _service.CreatePendingLoyaltyTransactionsAsync(order.Id, 300, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            account.AvailablePoints.Should().Be(200);
            addedRedeem.Should().NotBeNull();
            addedRedeem!.Points.Should().Be(300);
            addedRedeem.Type.Should().Be(LoyaltyTransactionType.Redeem);
            addedRedeem.Status.Should().Be(LoyaltyTransactionStatus.Pending);
            addedEarn.Should().NotBeNull();
            addedEarn!.Points.Should().Be(10);
            addedEarn.Type.Should().Be(LoyaltyTransactionType.Earn);
        }

        [Fact]
        public async Task CreatePendingLoyaltyTransactionsAsync_RedemptionInsufficientBalance_ReturnsFailure()
        {
            var order = CreateOrderWithSubtotal(100_000, OrderStatus.Confirmed);
            var account = LoyaltyAccount.Create(order.UserId);
            account.AddAvailablePoints(200);

            SetupOrder(order);
            SetupAccount(account);

            var result = await _service.CreatePendingLoyaltyTransactionsAsync(order.Id, 500, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainMatch("*Insufficient points*");
        }

        [Fact]
        public async Task CompletePendingTransactionsForOrderAsync_WhenOrderIsNotCompleted_ReturnsFailure()
        {
            var order = CreateOrderWithSubtotal(25_000, OrderStatus.Delivered);
            SetupOrder(order);

            var result = await _service.CompletePendingTransactionsForOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Points can only be completed for completed orders.");
        }

        [Fact]
        public async Task CompletePendingTransactionsForOrderAsync_CompletesBothEarnAndRedeem()
        {
            var order = CreateOrderWithSubtotal(25_000, OrderStatus.Completed);
            var account = LoyaltyAccount.Create(order.UserId);
            account.AddPendingPoints(2);
            var earnTx = LoyaltyTransaction.CreatePendingEarn(account.Id, order.Id, 2);
            var redeemTx = LoyaltyTransaction.CreatePendingRedeem(account.Id, order.Id, 300);
            order.LoyaltyTransactions.Add(earnTx);
            order.LoyaltyTransactions.Add(redeemTx);

            SetupOrder(order);
            _accountRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<LoyaltyAccount, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<LoyaltyAccount, object>>[]>()))
                .ReturnsAsync(account);
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _service.CompletePendingTransactionsForOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(302);
            earnTx.Status.Should().Be(LoyaltyTransactionStatus.Completed);
            redeemTx.Status.Should().Be(LoyaltyTransactionStatus.Completed);
            account.PendingPoints.Should().Be(0);
            account.AvailablePoints.Should().Be(2);
        }

        [Fact]
        public async Task CancelPendingTransactionsForOrderAsync_CancelsEarnAndRefundsRedeem()
        {
            var order = CreateOrderWithSubtotal(25_000, OrderStatus.Cancelled);
            var account = LoyaltyAccount.Create(order.UserId);
            account.AddPendingPoints(2);
            account.AddAvailablePoints(100);
            var earnTx = LoyaltyTransaction.CreatePendingEarn(account.Id, order.Id, 2);
            var redeemTx = LoyaltyTransaction.CreatePendingRedeem(account.Id, order.Id, 300);
            order.LoyaltyTransactions.Add(earnTx);
            order.LoyaltyTransactions.Add(redeemTx);

            SetupOrder(order);
            _accountRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<LoyaltyAccount, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<LoyaltyAccount, object>>[]>()))
                .ReturnsAsync(account);
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _service.CancelPendingTransactionsForOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(302);
            earnTx.Status.Should().Be(LoyaltyTransactionStatus.Cancelled);
            redeemTx.Status.Should().Be(LoyaltyTransactionStatus.Cancelled);
            account.AvailablePoints.Should().Be(400);
        }

        [Fact]
        public async Task CancelPendingTransactionsForOrderAsync_NoPendingTransactions_ReturnZero()
        {
            var order = CreateOrderWithSubtotal(25_000, OrderStatus.Cancelled);
            SetupOrder(order);

            var result = await _service.CancelPendingTransactionsForOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(0);
        }

        [Fact]
        public async Task CreatePendingLoyaltyTransactionsAsync_DuplicateEarnInsert_ReturnsSuccess()
        {
            var order = CreateOrderWithSubtotal(25_000, OrderStatus.Confirmed);
            var account = LoyaltyAccount.Create(order.UserId);
            var exception = new Exception("duplicate earn transaction");

            SetupOrder(order);
            SetupAccount(account);
            _transactionRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<LoyaltyTransaction>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            _uniqueConstraintCheckerMock
                .Setup(c => c.IsUniqueViolation(exception, "IX_LoyaltyTransactions_OrderId_Type"))
                .Returns(true);

            var result = await _service.CreatePendingLoyaltyTransactionsAsync(order.Id, null, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            result.Value.Should().Be(2);
        }

        private void SetupOrder(Order? order)
        {
            _orderRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Order, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Order, object>>[]>()))
                .ReturnsAsync(order);
        }

        private void SetupAccount(LoyaltyAccount? account)
        {
            _accountRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<LoyaltyAccount, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<LoyaltyAccount, object>>[]>()))
                .ReturnsAsync(account);
        }

        private static Order CreateOrderWithSubtotal(long subtotal, OrderStatus status)
        {
            var order = Order.Create(Guid.NewGuid(), 100001, PaymentMethod.COD);
            order.ClearDomainEvents();
            order.AddItem(Guid.NewGuid(), 1, new Money(subtotal), "snapshot");
            SetOrderStatus(order, status);

            return order;
        }

        private static void SetOrderStatus(Order order, OrderStatus status)
        {
            typeof(Order)
                .GetProperty(nameof(Order.Status), BindingFlags.Instance | BindingFlags.Public)!
                .SetValue(order, status);
        }
    }
}

using Application.DTOs.Loyalty;
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
        private readonly LoyaltyService _service;

        public LoyaltyServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _uniqueConstraintCheckerMock = new Mock<IUniqueConstraintChecker>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _orderRepositoryMock = new Mock<IGenericRepository<Order>>();
            _accountRepositoryMock = new Mock<IGenericRepository<LoyaltyAccount>>();
            _transactionRepositoryMock = new Mock<IGenericRepository<LoyaltyTransaction>>();

            _unitOfWorkMock
                .Setup(u => u.GetRepository<Order>())
                .Returns(_orderRepositoryMock.Object);
            _unitOfWorkMock
                .Setup(u => u.GetRepository<LoyaltyAccount>())
                .Returns(_accountRepositoryMock.Object);
            _unitOfWorkMock
                .Setup(u => u.GetRepository<LoyaltyTransaction>())
                .Returns(_transactionRepositoryMock.Object);

            _service = new LoyaltyService(_unitOfWorkMock.Object, _uniqueConstraintCheckerMock.Object, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task AwardPendingPointsForDeliveredOrderAsync_WhenOrderIdIsEmpty_ReturnsFailure()
        {
            var result = await _service.AwardPendingPointsForDeliveredOrderAsync(Guid.Empty, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Order ID cannot be empty.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AwardPendingPointsForDeliveredOrderAsync_WhenOrderNotFound_ReturnsNotFound()
        {
            SetupOrder(null);

            var result = await _service.AwardPendingPointsForDeliveredOrderAsync(Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Order not found.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AwardPendingPointsForDeliveredOrderAsync_WhenOrderIsNotDelivered_ReturnsFailure()
        {
            var order = CreateOrderWithSubtotal(25_000, OrderStatus.Confirmed);
            SetupOrder(order);

            var result = await _service.AwardPendingPointsForDeliveredOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Points can only be awarded for delivered orders.");
            _transactionRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<LoyaltyTransaction>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AwardPendingPointsForDeliveredOrderAsync_WhenSubtotalIsBelowMinimum_ReturnsZeroAndWritesNothing()
        {
            var order = CreateOrderWithSubtotal(9_999, OrderStatus.Delivered);
            SetupOrder(order);
            SetupExistingEarnTransaction(null);

            var result = await _service.AwardPendingPointsForDeliveredOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(0);
            _accountRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<LoyaltyAccount>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _transactionRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<LoyaltyTransaction>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AwardPendingPointsForDeliveredOrderAsync_WhenFirstEarnedOrder_CreatesAccountAndPendingTransaction()
        {
            var order = CreateOrderWithSubtotal(25_000, OrderStatus.Delivered);
            LoyaltyAccount? addedAccount = null;
            LoyaltyTransaction? addedTransaction = null;

            SetupOrder(order);
            SetupExistingEarnTransaction(null);
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

            var result = await _service.AwardPendingPointsForDeliveredOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(2);
            addedAccount.Should().NotBeNull();
            addedAccount!.UserId.Should().Be(order.UserId);
            addedAccount.PendingPoints.Should().Be(2);
            addedAccount.AvailablePoints.Should().Be(0);
            addedTransaction.Should().NotBeNull();
            addedTransaction!.LoyaltyAccountId.Should().Be(addedAccount.Id);
            addedTransaction.OrderId.Should().Be(order.Id);
            addedTransaction.Points.Should().Be(2);
            addedTransaction.Type.Should().Be(LoyaltyTransactionType.Earn);
            addedTransaction.Status.Should().Be(LoyaltyTransactionStatus.Pending);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AwardPendingPointsForDeliveredOrderAsync_WhenAccountExists_AddsPendingPointsAndTransaction()
        {
            var order = CreateOrderWithSubtotal(10_000, OrderStatus.Delivered);
            var account = LoyaltyAccount.Create(order.UserId);
            account.AddPendingPoints(3);
            LoyaltyTransaction? addedTransaction = null;

            SetupOrder(order);
            SetupExistingEarnTransaction(null);
            SetupAccount(account);
            _transactionRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<LoyaltyTransaction>(), It.IsAny<CancellationToken>()))
                .Callback<LoyaltyTransaction, CancellationToken>((transaction, _) => addedTransaction = transaction)
                .Returns(Task.CompletedTask);
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _service.AwardPendingPointsForDeliveredOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(1);
            account.PendingPoints.Should().Be(4);
            addedTransaction.Should().NotBeNull();
            addedTransaction!.Points.Should().Be(1);
            _accountRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<LoyaltyAccount>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AwardPendingPointsForDeliveredOrderAsync_WhenEarnTransactionAlreadyExists_ReturnsZeroAndDoesNothing()
        {
            var order = CreateOrderWithSubtotal(25_000, OrderStatus.Delivered);
            var account = LoyaltyAccount.Create(order.UserId);
            var existingTransaction = LoyaltyTransaction.CreatePendingEarn(account.Id, order.Id, 2);

            SetupOrder(order);
            SetupExistingEarnTransaction(existingTransaction);

            var result = await _service.AwardPendingPointsForDeliveredOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(0);
            _accountRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<LoyaltyAccount>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _transactionRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<LoyaltyTransaction>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AwardPendingPointsForDeliveredOrderAsync_WhenDuplicateEarnInsertRaceOccurs_ReturnsZero()
        {
            var order = CreateOrderWithSubtotal(25_000, OrderStatus.Delivered);
            var account = LoyaltyAccount.Create(order.UserId);
            var exception = new Exception("duplicate earn transaction");

            SetupOrder(order);
            SetupExistingEarnTransaction(null);
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

            var result = await _service.AwardPendingPointsForDeliveredOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(0);
        }

        [Fact]
        public async Task CompletePendingTransactionsForOrderAsync_WhenOrderIsNotCompleted_ReturnsFailure()
        {
            var order = CreateOrderWithSubtotal(25_000, OrderStatus.Delivered);
            SetupOrder(order);

            var result = await _service.CompletePendingTransactionsForOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Points can only be completed for completed orders.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompletePendingTransactionsForOrderAsync_WhenCompletedOrderHasPendingTransactions_CompletesThemAndUpdatesAccountPoints()
        {
            var order = CreateOrderWithSubtotal(25_000, OrderStatus.Completed);
            var account = LoyaltyAccount.Create(order.UserId);
            account.AddPendingPoints(2);
            var transaction = LoyaltyTransaction.CreatePendingEarn(account.Id, order.Id, 2);
            order.LoyaltyTransactions.Add(transaction);

            SetupOrder(order);
            _accountRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<LoyaltyAccount, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<LoyaltyAccount, object>>[]>() ))
                .ReturnsAsync(account);
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _service.CompletePendingTransactionsForOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(2);
            transaction.Status.Should().Be(LoyaltyTransactionStatus.Completed);
            account.PendingPoints.Should().Be(0);
            account.AvailablePoints.Should().Be(2);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RedeemPointsAtCheckoutAsync_ValidRequest_DeductsAndCreatesTransaction()
        {
            var userId = Guid.NewGuid();
            var order = CreateOrderWithSubtotal(100_000, OrderStatus.Pending);
            var account = LoyaltyAccount.Create(userId);
            account.AddAvailablePoints(500);
            LoyaltyTransaction? addedTransaction = null;

            SetupCurrentUser(userId);
            SetupAccount(account);
            SetupOrder(order);
            _transactionRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<LoyaltyTransaction>(), It.IsAny<CancellationToken>()))
                .Callback<LoyaltyTransaction, CancellationToken>((t, _) => addedTransaction = t)
                .Returns(Task.CompletedTask);
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _service.RedeemPointsAtCheckoutAsync(
                new RedeemPointsRequest(order.Id, 300), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value?.RedeemedPoints.Should().Be(300);
            result.Value?.DiscountAmount.Should().Be(30_000);
            result.Value?.RemainingBalance.Should().Be(200);
            account.AvailablePoints.Should().Be(200);
            order.DiscountAmount.Should().Be(30_000);
            addedTransaction.Should().NotBeNull();
            addedTransaction!.Points.Should().Be(300);
            addedTransaction.Type.Should().Be(LoyaltyTransactionType.Redeem);
            addedTransaction.Status.Should().Be(LoyaltyTransactionStatus.Pending);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RedeemPointsAtCheckoutAsync_PointsNotMultipleOf100_ReturnsFailure()
        {
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);

            var result = await _service.RedeemPointsAtCheckoutAsync(
                new RedeemPointsRequest(Guid.NewGuid(), 250), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Redeemed points must be in multiples of 100.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RedeemPointsAtCheckoutAsync_InsufficientBalance_ReturnsFailure()
        {
            var userId = Guid.NewGuid();
            var account = LoyaltyAccount.Create(userId);
            account.AddAvailablePoints(200);

            SetupCurrentUser(userId);
            SetupAccount(account);

            var result = await _service.RedeemPointsAtCheckoutAsync(
                new RedeemPointsRequest(Guid.NewGuid(), 500), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainMatch("*Insufficient points*");
            _accountRepositoryMock.Verify(r => r.FindAsync(
                It.IsAny<Expression<Func<LoyaltyAccount, bool>>>(),
                false, It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<LoyaltyAccount, object>>[]>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RedeemPointsAtCheckoutAsync_ExcessivePoints_CappedToMinimumOrder()
        {
            var userId = Guid.NewGuid();
            var order = CreateOrderWithSubtotal(50_000, OrderStatus.Pending);
            var account = LoyaltyAccount.Create(userId);
            account.AddAvailablePoints(1000);

            SetupCurrentUser(userId);
            SetupAccount(account);
            SetupOrder(order);
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _service.RedeemPointsAtCheckoutAsync(
                new RedeemPointsRequest(order.Id, 800), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value?.RedeemedPoints.Should().Be(400);
            result.Value?.DiscountAmount.Should().Be(40_000);
            result.Value?.RemainingBalance.Should().Be(600);
            account.AvailablePoints.Should().Be(600);
            order.DiscountAmount.Should().Be(40_000);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RedeemPointsAtCheckoutAsync_OrderNotFound_ReturnsNotFound()
        {
            var userId = Guid.NewGuid();
            var account = LoyaltyAccount.Create(userId);
            account.AddAvailablePoints(500);

            SetupCurrentUser(userId);
            SetupAccount(account);
            SetupOrder(null);

            var result = await _service.RedeemPointsAtCheckoutAsync(
                new RedeemPointsRequest(Guid.NewGuid(), 300), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Order not found.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RedeemPointsAtCheckoutAsync_AccountNotFound_ReturnsFailure()
        {
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);
            SetupAccount(null);

            var result = await _service.RedeemPointsAtCheckoutAsync(
                new RedeemPointsRequest(Guid.NewGuid(), 300), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainMatch("*Loyalty account not found*");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RedeemPointsAtCheckoutAsync_WhenMinimumOrderCannotBeMet_ReturnsFailure()
        {
            var userId = Guid.NewGuid();
            var order = CreateOrderWithSubtotal(15_000, OrderStatus.Pending);
            var account = LoyaltyAccount.Create(userId);
            account.AddAvailablePoints(500);

            SetupCurrentUser(userId);
            SetupAccount(account);
            SetupOrder(order);

            var result = await _service.RedeemPointsAtCheckoutAsync(
                new RedeemPointsRequest(order.Id, 200), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainMatch("*Redemption would reduce order total below minimum*");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteRedeemedPointsForOrderAsync_DeliveredOrder_CompletesTransactions()
        {
            var order = CreateOrderWithSubtotal(100_000, OrderStatus.Delivered);
            var transaction = LoyaltyTransaction.CreatePendingRedeem(Guid.NewGuid(), order.Id, 300);
            order.LoyaltyTransactions.Add(transaction);

            SetupOrder(order);
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _service.CompleteRedeemedPointsForOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(300);
            transaction.Status.Should().Be(LoyaltyTransactionStatus.Completed);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CompleteRedeemedPointsForOrderAsync_NotDelivered_ReturnsFailure()
        {
            var order = CreateOrderWithSubtotal(100_000, OrderStatus.Confirmed);
            SetupOrder(order);

            var result = await _service.CompleteRedeemedPointsForOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Redeemed points can only be finalized for delivered orders.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteRedeemedPointsForOrderAsync_NoRedeemTransactions_NoOp()
        {
            var order = CreateOrderWithSubtotal(100_000, OrderStatus.Delivered);
            SetupOrder(order);

            var result = await _service.CompleteRedeemedPointsForOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(0);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        private void SetupCurrentUser(Guid userId)
        {
            _currentUserServiceMock
                .Setup(s => s.GetUserIdOrNull())
                .Returns(userId.ToString());
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

        private void SetupExistingEarnTransaction(LoyaltyTransaction? transaction)
        {
            _transactionRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<LoyaltyTransaction, bool>>>(),
                    true,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<LoyaltyTransaction, object>>[]>()))
                .ReturnsAsync(transaction);
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

using Application.Common.Response;
using Application.DTOs.Loyalty;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public sealed class LoyaltyService : ILoyaltyService
    {
        private const int VndPerPoint = 10_000;
        private const string EarnTransactionUniqueIndex = "IX_LoyaltyTransactions_OrderId_Type";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IUniqueConstraintChecker _uniqueConstraintChecker;

        public LoyaltyService(
            IUnitOfWork unitOfWork,
            IUniqueConstraintChecker uniqueConstraintChecker)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _uniqueConstraintChecker = uniqueConstraintChecker ?? throw new ArgumentNullException(nameof(uniqueConstraintChecker));
        }

        public async Task<Result<int>> AwardPendingPointsForDeliveredOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            if (orderId == Guid.Empty)
            {
                return Result<int>.Failure("Order ID cannot be empty.");
            }

            var order = await _unitOfWork.GetRepository<Order>()
                .FindAsync(
                    o => o.Id == orderId && !o.IsDeleted,
                    asNoTracking: false,
                    cancellationToken,
                    o => o.OrderItems);

            if (order == null)
            {
                return Result<int>.NotFound("Order not found.");
            }

            if (order.Status != OrderStatus.Delivered)
            {
                return Result<int>.Failure("Points can only be awarded for delivered orders.");
            }

            var existingEarnTransaction = await _unitOfWork.GetRepository<LoyaltyTransaction>()
                .FindAsync(
                    t => t.OrderId == orderId && t.Type == LoyaltyTransactionType.Earn,
                    asNoTracking: true,
                    cancellationToken);

            if (existingEarnTransaction != null)
            {
                return Result<int>.Success(0);
            }

            var points = CalculateEarnedPoints(order);
            if (points <= 0)
            {
                return Result<int>.Success(0);
            }

            var account = await _unitOfWork.GetRepository<LoyaltyAccount>()
                .FindAsync(
                    a => a.UserId == order.UserId && !a.IsDeleted,
                    asNoTracking: false,
                    cancellationToken);

            if (account == null)
            {
                account = LoyaltyAccount.Create(order.UserId);
                await _unitOfWork.GetRepository<LoyaltyAccount>().AddAsync(account, cancellationToken);
            }

            account.AddPendingPoints(points);

            var transaction = LoyaltyTransaction.CreatePendingEarn(account.Id, order.Id, points);
            await _unitOfWork.GetRepository<LoyaltyTransaction>().AddAsync(transaction, cancellationToken);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (_uniqueConstraintChecker.IsUniqueViolation(ex, EarnTransactionUniqueIndex))
            {
                return Result<int>.Success(0);
            }

            return Result<int>.Success(points);
        }

        public async Task<Result<int>> CompletePendingTransactionsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            if (orderId == Guid.Empty)
            {
                return Result<int>.Failure("Order ID cannot be empty.");
            }

            var order = await _unitOfWork.GetRepository<Order>()
                .FindAsync(
                    o => o.Id == orderId && !o.IsDeleted,
                    asNoTracking: false,
                    cancellationToken,
                    o => o.LoyaltyTransactions);

            if (order == null)
            {
                return Result<int>.NotFound("Order not found.");
            }

            if (order.Status != OrderStatus.Completed)
            {
                return Result<int>.Failure("Points can only be completed for completed orders.");
            }

            var pendingTransactions = order.LoyaltyTransactions
                .Where(t => t.Status == LoyaltyTransactionStatus.Pending && t.Type == LoyaltyTransactionType.Earn)
                .ToList();

            if (!pendingTransactions.Any())
            {
                return Result<int>.Success(0);
            }

            var account = await _unitOfWork.GetRepository<LoyaltyAccount>()
                .FindAsync(
                    a => a.Id == pendingTransactions.First().LoyaltyAccountId && !a.IsDeleted,
                    asNoTracking: false,
                    cancellationToken);

            if (account == null)
            {
                return Result<int>.Failure("Loyalty account not found.");
            }

            var totalPoints = pendingTransactions.Sum(t => t.Points);
            foreach (var transaction in pendingTransactions)
            {
                transaction.Complete();
            }

            account.CompletePendingPoints(totalPoints);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(totalPoints);

        }

        public async Task<Result<GetLoyaltyBalanceResponse>> GetLoyaltyBalanceAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userIdGuid))
            {
                return Result<GetLoyaltyBalanceResponse>.Unauthorized("Invalid user ID.");
            }

            var account = await _unitOfWork.GetRepository<LoyaltyAccount>()
                .FindAsync(
                    a => a.UserId == userIdGuid && !a.IsDeleted,
                    asNoTracking: true,
                    cancellationToken);

            if (account == null)
            {
                // Return zero balance for user with no loyalty account
                return Result<GetLoyaltyBalanceResponse>.Success(
                    new GetLoyaltyBalanceResponse(
                        Balance: 0,
                        VndEquivalent: 0,
                        LastUpdated: DateTime.UtcNow));
            }

            var totalBalance = account.AvailablePoints + account.PendingPoints;
            
            // Handle negative balance edge case
            if (totalBalance < 0)
            {
                totalBalance = 0;
            }

            var vndEquivalent = totalBalance * VndPerPoint / 100;

            return Result<GetLoyaltyBalanceResponse>.Success(
                new GetLoyaltyBalanceResponse(
                    Balance: totalBalance,
                    VndEquivalent: vndEquivalent,
                    LastUpdated: DateTime.UtcNow));
        }

        public async Task<Result<GetLoyaltyTransactionsResponse>> GetTransactionHistoryAsync(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userIdGuid))
            {
                return Result<GetLoyaltyTransactionsResponse>.Unauthorized("Invalid user ID.");
            }

            // Validate pagination
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1 || pageSize > 100)
                pageSize = 10;

            var account = await _unitOfWork.GetRepository<LoyaltyAccount>()
                .FindAsync(
                    a => a.UserId == userIdGuid && !a.IsDeleted,
                    asNoTracking: true,
                    cancellationToken);

            if (account == null)
            {
                // Return empty response for user with no loyalty account
                return Result<GetLoyaltyTransactionsResponse>.Success(
                    new GetLoyaltyTransactionsResponse(
                        Transactions: new List<LoyaltyTransactionDto>(),
                        TotalCount: 0,
                        PageNumber: pageNumber,
                        PageSize: pageSize));
            }

            var (transactions, totalCount) = await _unitOfWork.GetRepository<LoyaltyTransaction>()
                .GetPagedAsync(
                    page: pageNumber,
                    pageSize: pageSize,
                    filter: t => t.LoyaltyAccountId == account.Id && !t.IsDeleted,
                    orderBy: t => t.CreatedAt,
                    isDescending: true,
                    cancellationToken: cancellationToken);

            var transactionDtos = transactions.Select(t => new LoyaltyTransactionDto(
                Id: t.Id,
                Date: t.CreatedAt,
                Type: t.Type.ToString(),
                Points: t.Type == LoyaltyTransactionType.Earn ? t.Points : -t.Points,
                OrderId: t.OrderId?.ToString(),
                Description: t.Description
            )).ToList();

            return Result<GetLoyaltyTransactionsResponse>.Success(
                new GetLoyaltyTransactionsResponse(
                    Transactions: transactionDtos,
                    TotalCount: totalCount,
                    PageNumber: pageNumber,
                    PageSize: pageSize));
        }

        private static int CalculateEarnedPoints(Order order)
        {
            var subtotal = order.OrderItems.Sum(item => item.Price.Amount * item.Quantity);
            return (int)(subtotal / VndPerPoint);
        }
    }
}

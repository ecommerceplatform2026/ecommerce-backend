using Application.Common.Response;
using Application.DTOs.Loyalty;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Application.Mappings;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public sealed class LoyaltyService : ILoyaltyService
    {
        public static int PointEarnRate => 1 / 10_000;    // 10,000 VND spent = 1 point
        public static int PointRedeemRate => 100;         // 1 point = 100 VND discount
        public static int PointPerRedeemUnit => 100;      // Points must be redeemed in multiples of 100

        private const string EarnTransactionUniqueIndex = "IX_LoyaltyTransactions_OrderId_Type";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IUniqueConstraintChecker _uniqueConstraintChecker;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;

        public LoyaltyService(
            IUnitOfWork unitOfWork,
            IUniqueConstraintChecker uniqueConstraintChecker,
            ICurrentUserService currentUserService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _uniqueConstraintChecker = uniqueConstraintChecker ?? throw new ArgumentNullException(nameof(uniqueConstraintChecker));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async Task<Result<int>> CreatePendingLoyaltyTransactionsAsync(
            Guid orderId, int? redeemedPoints, CancellationToken cancellationToken = default)
        {
            if (orderId == Guid.Empty)
                return Result<int>.Failure("Order ID cannot be empty.");

            var order = await _unitOfWork.GetRepository<Order>()
                .FindAsync(
                    o => o.Id == orderId && !o.IsDeleted,
                    asNoTracking: false,
                    cancellationToken,
                    o => o.OrderItems);

            if (order == null)
                return Result<int>.NotFound("Order not found.");

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

            var totalEarned = 0;

            var earnPoints = CalculateEarnedPoints(order);
            if (earnPoints > 0)
            {
                account.AddPendingPoints(earnPoints);

                var earnTransaction = LoyaltyTransaction.CreatePendingEarn(account.Id, order.Id, earnPoints);
                await _unitOfWork.GetRepository<LoyaltyTransaction>().AddAsync(earnTransaction, cancellationToken);
                totalEarned = earnPoints;
            }

            if (redeemedPoints.HasValue && redeemedPoints.Value > 0)
            {
                var points = redeemedPoints.Value;

                if (account.AvailablePoints < points)
                    return Result<int>.Failure($"Insufficient points. You have {account.AvailablePoints} points but attempted to redeem {points}.");

                account.DeductAvailablePoints(points);

                var redeemTransaction = LoyaltyTransaction.CreatePendingRedeem(account.Id, order.Id, points);
                await _unitOfWork.GetRepository<LoyaltyTransaction>().AddAsync(redeemTransaction, cancellationToken);
            }

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (_uniqueConstraintChecker.IsUniqueViolation(ex, EarnTransactionUniqueIndex))
            {
                return Result<int>.Success(totalEarned);
            }

            var cancelled = await CancelPendingExpiredTransactionsAsync(account, cancellationToken);
            if (cancelled)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result<int>.Success(earnPoints);
        }

        public async Task<Result<int>> CompletePendingTransactionsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            if (orderId == Guid.Empty)
                return Result<int>.Failure("Order ID cannot be empty.");

            var order = await _unitOfWork.GetRepository<Order>()
                .FindAsync(
                    o => o.Id == orderId && !o.IsDeleted,
                    asNoTracking: false,
                    cancellationToken,
                    o => o.LoyaltyTransactions);

            if (order == null)
                return Result<int>.NotFound("Order not found.");

            if (order.Status != OrderStatus.Completed)
                return Result<int>.Failure("Points can only be completed for completed orders.");

            var pendingTransactions = order.LoyaltyTransactions
                .Where(t => t.Status == LoyaltyTransactionStatus.Pending)
                .ToList();

            if (pendingTransactions.Count == 0)
                return Result<int>.Success(0);

            var account = await _unitOfWork.GetRepository<LoyaltyAccount>()
                .FindAsync(
                    a => a.Id == pendingTransactions.First().LoyaltyAccountId && !a.IsDeleted,
                    asNoTracking: false,
                    cancellationToken);

            if (account == null)
                return Result<int>.Failure("Loyalty account not found.");

            var totalEarnPoints = 0;

            foreach (var transaction in pendingTransactions)
            {
                transaction.Complete();
                if (transaction.Type == LoyaltyTransactionType.Earn)
                {
                    totalEarnPoints += transaction.Points;
                }
            }

            if (totalEarnPoints > 0)
            {
                account.CompletePendingPoints(totalEarnPoints);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(pendingTransactions.Sum(t => t.Points));
        }

        public async Task<Result<int>> CancelPendingTransactionsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            if (orderId == Guid.Empty)
                return Result<int>.Failure("Order ID cannot be empty.");

            var order = await _unitOfWork.GetRepository<Order>()
                .FindAsync(
                    o => o.Id == orderId && !o.IsDeleted,
                    asNoTracking: false,
                    cancellationToken,
                    o => o.LoyaltyTransactions);

            if (order == null)
                return Result<int>.NotFound("Order not found.");

            var pendingTransactions = order.LoyaltyTransactions
                .Where(t => t.Status == LoyaltyTransactionStatus.Pending)
                .ToList();

            if (pendingTransactions.Count == 0)
                return Result<int>.Success(0);

            var account = await _unitOfWork.GetRepository<LoyaltyAccount>()
                .FindAsync(
                    a => a.Id == pendingTransactions.First().LoyaltyAccountId && !a.IsDeleted,
                    asNoTracking: false,
                    cancellationToken);

            if (account == null)
                return Result<int>.Failure("Loyalty account not found.");

            foreach (var transaction in pendingTransactions)
            {
                transaction.Cancel();

                if (transaction.Type == LoyaltyTransactionType.Earn)
                {
                    account.DeductPendingPoints(transaction.Points);
                }
                else if (transaction.Type == LoyaltyTransactionType.Redeem)
                {
                    account.AddAvailablePoints(transaction.Points);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(pendingTransactions.Sum(t => t.Points));
        }

        public async Task<Result<int>> ExpireInactivePointsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var twelveMonthsAgo = now.AddMonths(-12);
            var elevenMonthsAgo = now.AddMonths(-11);

            var accounts = await LoadAccountsWithPointsAsync(cancellationToken);
            if (!accounts.Any())
                return Result<int>.Success(0);

            var lastOrderDates = await LoadLastOrderDatesAsync(accounts, cancellationToken);
            var expiredTxMap = await LoadExpiredTransactionsAsync(accounts, cancellationToken);

            var totalExpired = 0;
            var txRepo = _unitOfWork.GetRepository<LoyaltyTransaction>();

            foreach (var account in accounts)
            {
                if (!lastOrderDates.TryGetValue(account.UserId, out var lastOrderDate))
                    continue;

                if (lastOrderDate >= elevenMonthsAgo)
                    continue;

                var existingExpired = expiredTxMap.GetValueOrDefault(account.Id) ?? new List<LoyaltyTransaction>();

                if (lastOrderDate < twelveMonthsAgo)
                {
                    var expired = await ExpireAllPointsAsync(account, existingExpired, txRepo, cancellationToken);
                    totalExpired += expired;
                }
                else
                {
                    await SendWarningIfNeededAsync(account, existingExpired, lastOrderDate, now, cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(totalExpired);
        }

        private async Task<List<LoyaltyAccount>> LoadAccountsWithPointsAsync(CancellationToken ct)
        {
            return (await _unitOfWork.GetRepository<LoyaltyAccount>()
                .GetAllAsync(a => !a.IsDeleted && a.AvailablePoints > 0, ct)).ToList();
        }

        private async Task<Dictionary<Guid, DateTime>> LoadLastOrderDatesAsync(
            List<LoyaltyAccount> accounts, CancellationToken ct)
        {
            var userIds = accounts.Select(a => a.UserId).ToList();

            var orders = await _unitOfWork.GetRepository<Order>().GetAllAsync(
                o => userIds.Contains(o.UserId)
                    && o.Status == OrderStatus.Delivered
                    && !o.IsDeleted,
                ct);

            return orders
                .GroupBy(o => o.UserId)
                .ToDictionary(g => g.Key, g => g.Max(o => o.CreatedAt));
        }

        private async Task<Dictionary<Guid, List<LoyaltyTransaction>>> LoadExpiredTransactionsAsync(
            List<LoyaltyAccount> accounts, CancellationToken ct)
        {
            var accountIds = accounts.Select(a => a.Id).ToList();

            var txs = await _unitOfWork.GetRepository<LoyaltyTransaction>().GetAllAsync(
                t => accountIds.Contains(t.LoyaltyAccountId)
                    && t.Type == LoyaltyTransactionType.Expired,
                ct);

            return txs
                .GroupBy(t => t.LoyaltyAccountId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        private async Task<int> ExpireAllPointsAsync(
            LoyaltyAccount account,
            List<LoyaltyTransaction> existingExpired,
            IGenericRepository<LoyaltyTransaction> txRepo,
            CancellationToken ct)
        {
            if (existingExpired.Any(t => t.Status == LoyaltyTransactionStatus.Completed))
                return 0;

            var pending = existingExpired.FirstOrDefault(t => t.Status == LoyaltyTransactionStatus.Pending);
            if (pending != null)
            {
                pending.Complete();
                account.ExpirePoints(pending.Points);
                return pending.Points;
            }

            var tx = LoyaltyTransaction.CreateExpired(account.Id, account.AvailablePoints);
            await txRepo.AddAsync(tx, ct);
            account.ExpirePoints(account.AvailablePoints);
            return tx.Points;
        }

        private async Task SendWarningIfNeededAsync(
            LoyaltyAccount account,
            List<LoyaltyTransaction> existingExpired,
            DateTime lastOrderDate,
            DateTime now,
            CancellationToken ct)
        {
            var hasRecentWarning = existingExpired.Any(t => t.CreatedAt >= now.AddDays(-30));
            if (hasRecentWarning)
                return;

            var pendingTx = LoyaltyTransaction.CreatePendingExpired(account.Id, account.AvailablePoints);
            await _unitOfWork.GetRepository<LoyaltyTransaction>().AddAsync(pendingTx, ct);

            var expiryDate = lastOrderDate.AddMonths(12);
            await _notificationService.SendPointsExpiryWarningAsync(
                account.UserId, account.AvailablePoints, expiryDate);
        }

        private async Task<bool> CancelPendingExpiredTransactionsAsync(LoyaltyAccount account, CancellationToken cancellationToken)
        {
            var pendingExpired = await _unitOfWork.GetRepository<LoyaltyTransaction>()
                .FindAsync(
                    t => t.LoyaltyAccountId == account.Id
                        && t.Type == LoyaltyTransactionType.Expired
                        && t.Status == LoyaltyTransactionStatus.Pending,
                    asNoTracking: false,
                    cancellationToken);

            if (pendingExpired == null)
                return false;

            pendingExpired.Cancel();
            account.AddAvailablePoints(pendingExpired.Points);
            return true;
        }

        /// <summary>
        /// Calculates the loyalty points earned from a purchase amount.
        /// Points are determined by dividing the purchase amount by the earn rate,
        /// then rounding down (floor) so that partial points are not awarded.
        /// </summary>
        /// <param name="amount">The purchase amount in VND.</param>
        /// <param name="earnRate">The earn rate: amount of VND required to earn 1 point. Default is 10,000 VND/point.</param>
        /// <returns>The number of points earned (always ≥ 0).</returns>
        public static int CalculateEarnValue(long amount)
        {
            return (int)(amount * PointEarnRate);
        }

        /// <summary>
        /// Calculates the discount value (in VND) obtained by redeeming loyalty points.
        /// Each point is multiplied by the redeem rate to determine the total discount.
        /// For example, with a redeem rate of 100 VND/point, redeeming 200 points yields 20,000 VND off.
        /// </summary>
        /// <param name="points">The number of points to redeem.</param>
        /// <param name="redeemRate">The redeem rate: VND discount value per 1 point. Default is 100 VND/point.</param>
        /// <returns>The total discount value in VND.</returns>
        public static int CalculateRedeemValue(int points)
        {
            return points * PointRedeemRate;
        }

        public static void ValidateRedemptionPoints(int points)
        {
            if (points % PointPerRedeemUnit != 0)
                throw new InvalidOperationException($"Redeemed points must be in multiples of {PointPerRedeemUnit}.");
        }

        public async Task<Result<GetLoyaltyBalanceResponse>> GetLoyaltyBalanceAsync(CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userIdGuid))
            {
                return Result<GetLoyaltyBalanceResponse>.Unauthorized("User is not authenticated.");
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
                        DiscountEquivalent: 0,
                        LastUpdated: DateTime.UtcNow));
            }

            var totalBalance = account.AvailablePoints + account.PendingPoints;
            
            // Handle negative balance edge case
            if (totalBalance < 0)
            {
                totalBalance = 0;
            }

            var vndEquivalent = totalBalance / PointEarnRate;

            return Result<GetLoyaltyBalanceResponse>.Success(
                new GetLoyaltyBalanceResponse(
                    Balance: totalBalance,
                    DiscountEquivalent: vndEquivalent,
                    LastUpdated: DateTime.UtcNow));
        }

        public async Task<Result<PagedResult<GetLoyaltyTransactionResponse>>> GetTransactionHistoryAsync(GetLoyaltyTransactionsRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Result<PagedResult<GetLoyaltyTransactionResponse>>.Failure("Request cannot be null.");
            }

            var userId = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userIdGuid))
            {
                return Result<PagedResult<GetLoyaltyTransactionResponse>>.Unauthorized("User is not authenticated.");
            }

            var account = await _unitOfWork.GetRepository<LoyaltyAccount>()
                .FindAsync(
                    a => a.UserId == userIdGuid && !a.IsDeleted,
                    asNoTracking: true,
                    cancellationToken);

            if (account == null)
            {
                // Return empty response for user with no loyalty account
                return Result<PagedResult<GetLoyaltyTransactionResponse>>.Success(
                    new PagedResult<GetLoyaltyTransactionResponse>
                    {
                        Items = new List<GetLoyaltyTransactionResponse>(),
                        TotalCount = 0,
                        Page = request.Page,
                        PageSize = request.PageSize
                    });
            }

            System.Linq.Expressions.Expression<Func<LoyaltyTransaction, bool>> filter = t => t.LoyaltyAccountId == account.Id && !t.IsDeleted;
            if (request.Status.HasValue)
            {
                filter = t => t.Status == request.Status.Value;
            }
            if (request.Type.HasValue)
            {
                filter = t => t.Type == request.Type.Value;
            }

            var (transactions, totalCount) = await _unitOfWork.GetRepository<LoyaltyTransaction>()
                .GetPagedAsync(
                    page: request.Page,
                    pageSize: request.PageSize,
                    filter: filter,
                    orderBy: t => t.CreatedAt,
                    isDescending: true,
                    cancellationToken: cancellationToken);

            var result = new PagedResult<GetLoyaltyTransactionResponse>
            {
                Items = transactions.Select(t => t.ToLoyaltyTransactionResponse()).ToList(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return Result<PagedResult<GetLoyaltyTransactionResponse>>.Success(result);
        }

        private static int CalculateEarnedPoints(Order order)
        {
            var subtotal = order.OrderItems.Sum(item => item.Price.Amount * item.Quantity);
            return CalculateEarnValue(subtotal);
        }
    }
}

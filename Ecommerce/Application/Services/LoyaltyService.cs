using Application.Common.Response;
using Application.DTOs.Loyalty;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Application.Mappings;
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
        private readonly ICurrentUserService _currentUserService;

        public LoyaltyService(
            IUnitOfWork unitOfWork,
            IUniqueConstraintChecker uniqueConstraintChecker,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _uniqueConstraintChecker = uniqueConstraintChecker ?? throw new ArgumentNullException(nameof(uniqueConstraintChecker));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
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

            var vndEquivalent = totalBalance * VndPerPoint / 100;

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
            return (int)(subtotal / VndPerPoint);
        }
    }
}

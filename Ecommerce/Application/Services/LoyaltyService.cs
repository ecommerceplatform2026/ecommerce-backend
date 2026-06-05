using Application.Common.Response;
using Application.DTOs.Loyalty;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Security;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
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

        private Result<Guid> GetCurrentUserId()
        {
            var userIdStr = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Result<Guid>.Unauthorized("User is not authenticated.");
            }
            return Result<Guid>.Success(userId);
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

        public async Task<Result<RedeemPointsResponse>> RedeemPointsAtCheckoutAsync(RedeemPointsRequest request, CancellationToken cancellationToken = default)
        {
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true))
                return Result<RedeemPointsResponse>.Failure(string.Join("; ", validationResults.Select(v => v.ErrorMessage)));

            var userResult = GetCurrentUserId();
            if (!userResult.IsSuccess)
                return Result<RedeemPointsResponse>.Unauthorized(userResult.Errors.FirstOrDefault() ?? "Unauthorized");

            var points = request.Points;

            if (points % 100 != 0)
                return Result<RedeemPointsResponse>.Failure("Redeemed points must be in multiples of 100.");

            var account = await _unitOfWork.GetRepository<LoyaltyAccount>()
                .FindAsync(
                    a => a.UserId == userResult.Value && !a.IsDeleted,
                    asNoTracking: false,
                    cancellationToken);

            if (account == null)
                return Result<RedeemPointsResponse>.Failure("Loyalty account not found. No points available to redeem.");

            if (account.AvailablePoints < points)
                return Result<RedeemPointsResponse>.Failure($"Insufficient points. You have {account.AvailablePoints} points but attempted to redeem {points}.");

            var order = await _unitOfWork.GetRepository<Order>()
                .FindAsync(
                    o => o.Id == request.OrderId && !o.IsDeleted,
                    asNoTracking: false,
                    cancellationToken,
                    o => o.OrderItems);

            if (order == null)
                return Result<RedeemPointsResponse>.NotFound("Order not found.");

            var subtotal = order.OrderItems.Sum(item => item.Price.Amount * item.Quantity);
            var discount = CalculateRedeemValue(points);
            var minOrderTotal = VndPerPoint;

            if (subtotal - discount < minOrderTotal)
            {
                var maxAffordablePoints = (int)((subtotal - minOrderTotal) / VndPerPoint) * 100;
                if (maxAffordablePoints <= 0)
                    return Result<RedeemPointsResponse>.Failure($"Redemption would reduce order total below minimum. Order total after discount must be at least {minOrderTotal} VND.");

                points = maxAffordablePoints;
                discount = CalculateRedeemValue(points);
            }

            if (points == 0)
                return Result<RedeemPointsResponse>.Failure("Cannot redeem points: discount would exceed order total.");

            account.DeductAvailablePoints(points);

            var transaction = LoyaltyTransaction.CreatePendingRedeem(account.Id, order.Id, points);
            await _unitOfWork.GetRepository<LoyaltyTransaction>().AddAsync(transaction, cancellationToken);

            order.ApplyDiscount(discount);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<RedeemPointsResponse>.Success(new RedeemPointsResponse(
                points,
                discount,
                account.AvailablePoints));
        }

        public async Task<Result<int>> CompleteRedeemedPointsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
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

            if (order.Status != OrderStatus.Delivered)
                return Result<int>.Failure("Redeemed points can only be finalized for delivered orders.");

            var pendingRedeemTransactions = order.LoyaltyTransactions
                .Where(t => t.Status == LoyaltyTransactionStatus.Pending && t.Type == LoyaltyTransactionType.Redeem)
                .ToList();

            if (pendingRedeemTransactions.Count == 0)
                return Result<int>.Success(0);

            var totalPoints = pendingRedeemTransactions.Sum(t => t.Points);
            foreach (var transaction in pendingRedeemTransactions)
            {
                transaction.Complete();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(totalPoints);
        }

        public async Task<Result<int>> RefundRedeemedPointsForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
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

            if (order.Status != OrderStatus.Cancelled)
                return Result<int>.Failure("Redeemed points can only be refunded for cancelled orders.");

            var pendingRedeemTransactions = order.LoyaltyTransactions
                .Where(t => t.Status == LoyaltyTransactionStatus.Pending && t.Type == LoyaltyTransactionType.Redeem)
                .ToList();

            if (pendingRedeemTransactions.Count == 0)
                return Result<int>.Success(0);

            var account = await _unitOfWork.GetRepository<LoyaltyAccount>()
                .FindAsync(
                    a => a.Id == pendingRedeemTransactions.First().LoyaltyAccountId && !a.IsDeleted,
                    asNoTracking: false,
                    cancellationToken);

            if (account == null)
                return Result<int>.Failure("Loyalty account not found.");

            var totalPoints = pendingRedeemTransactions.Sum(t => t.Points);
            foreach (var transaction in pendingRedeemTransactions)
            {
                transaction.Cancel();
            }

            account.AddAvailablePoints(totalPoints);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(totalPoints);
        }

        public async Task<Result<int>> ReverseEarnedPointsForReturnedOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
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

            if (order.Status != OrderStatus.Returned)
                return Result<int>.Failure("Earned points can only be reversed for returned orders.");

            var earnTransactions = order.LoyaltyTransactions
                .Where(t => (t.Status == LoyaltyTransactionStatus.Pending || t.Status == LoyaltyTransactionStatus.Completed)
                    && t.Type == LoyaltyTransactionType.Earn)
                .ToList();

            if (earnTransactions.Count == 0)
                return Result<int>.Success(0);

            var totalPoints = earnTransactions.Sum(t => t.Points);

            var account = await _unitOfWork.GetRepository<LoyaltyAccount>()
                .FindAsync(
                    a => a.Id == earnTransactions.First().LoyaltyAccountId && !a.IsDeleted,
                    asNoTracking: false,
                    cancellationToken);

            if (account == null)
                return Result<int>.Failure("Loyalty account not found.");

            account.ReverseEarnedPoints(totalPoints);

            foreach (var transaction in earnTransactions)
            {
                transaction.Cancel();
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(totalPoints);
        }

        /// <summary>
        /// Calculates the loyalty points earned from a purchase amount.
        /// Points are determined by dividing the purchase amount by the earn rate,
        /// then rounding down (floor) so that partial points are not awarded.
        /// </summary>
        /// <param name="amount">The purchase amount in VND.</param>
        /// <param name="earnRate">The earn rate: amount of VND required to earn 1 point. Default is 10,000 VND/point.</param>
        /// <returns>The number of points earned (always ≥ 0).</returns>
        private static int CalculateEarnValue(long amount, int earnRate = VndPerPoint)
        {
            return (int)(amount / earnRate);
        }

        /// <summary>
        /// Calculates the discount value (in VND) obtained by redeeming loyalty points.
        /// Each point is multiplied by the redeem rate to determine the total discount.
        /// For example, with a redeem rate of 100 VND/point, redeeming 200 points yields 20,000 VND off.
        /// </summary>
        /// <param name="points">The number of points to redeem.</param>
        /// <param name="redeemRate">The redeem rate: VND discount value per 1 point. Default is 100 VND/point.</param>
        /// <returns>The total discount value in VND.</returns>
        private static int CalculateRedeemValue(int points, int redeemRate = VndPerPoint / 100)
        {
            return points * redeemRate;
        }

        private static int CalculateEarnedPoints(Order order)
        {
            var subtotal = order.OrderItems.Sum(item => item.Price.Amount * item.Quantity);
            return CalculateEarnValue(subtotal);
        }
    }
}

using Application.Common.Response;
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

        private static int CalculateEarnedPoints(Order order)
        {
            var subtotal = order.OrderItems.Sum(item => item.Price.Amount * item.Quantity);
            return (int)(subtotal / VndPerPoint);
        }
    }
}

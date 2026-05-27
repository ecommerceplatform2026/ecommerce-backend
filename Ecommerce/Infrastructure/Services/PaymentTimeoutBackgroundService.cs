using Application.Common.Caching;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public sealed class PaymentTimeoutBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PaymentTimeoutBackgroundService> _logger;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan PaymentTimeout = TimeSpan.FromMinutes(15);

        public PaymentTimeoutBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<PaymentTimeoutBackgroundService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Payment Timeout Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CancelExpiredPaymentsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while canceling expired payments.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }

            _logger.LogInformation("Payment Timeout Background Service stopped.");
        }

        private async Task CancelExpiredPaymentsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var expirationThreshold = DateTime.UtcNow.Subtract(PaymentTimeout);

            var expiredPayments = await unitOfWork.GetRepository<Payment>()
                .GetQueryable()
                .Include(p => p.Order)
                    .ThenInclude(o => o!.OrderItems)
                .Where(p => p.Status == PaymentStatus.Pending
                    && p.Order != null
                    && p.Order.Status == OrderStatus.Pending
                    && (p.Order.PaymentMethod == PaymentMethod.VNPay
                        || p.Order.PaymentMethod == PaymentMethod.MoMo
                        || p.Order.PaymentMethod == PaymentMethod.ZaloPay
                        || p.Order.PaymentMethod == PaymentMethod.PayOS)
                    && p.Order.CreatedAt <= expirationThreshold
                    && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            if (expiredPayments == null || !expiredPayments.Any())
            {
                return;
            }

            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            _logger.LogInformation("Found {Count} expired payments to cancel.", expiredPayments.Count);

            foreach (var payment in expiredPayments)
            {
                var strategy = unitOfWork.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        payment.Fail();
                        unitOfWork.GetRepository<Payment>().Update(payment);

                        var productIdsToInvalidate = new HashSet<Guid>();
                        var order = payment.Order;
                        if (order != null)
                        {
                            order.Cancel();
                            unitOfWork.GetRepository<Order>().Update(order);

                            foreach (var orderItem in order.OrderItems)
                            {
                                var variant = await unitOfWork.GetRepository<ProductVariant>()
                                    .FindAsync(pv => pv.Id == orderItem.ProductVariantId, asNoTracking: false, cancellationToken);

                                if (variant != null)
                                {
                                    variant.UpdateStock(variant.Stock + orderItem.Quantity);
                                    unitOfWork.GetRepository<ProductVariant>().Update(variant);

                                    productIdsToInvalidate.Add(variant.ProductId);
                                }
                            }
                        }

                        await unitOfWork.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);

                        if (productIdsToInvalidate.Any())
                        {
                            try
                            {
                                await cacheService.RemoveAsync(CacheKeys.ProductsAll, cancellationToken);
                                foreach (var productId in productIdsToInvalidate)
                                {
                                    await cacheService.RemoveAsync(CacheKeys.GetProductDetailKey(productId), cancellationToken);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to invalidate product cache after cancelling expired payment for order code {OrderCode}.", payment.OrderCode);
                            }
                        }

                        _logger.LogInformation("Successfully cancelled expired order {OrderCode} and restored inventory.", payment.OrderCode);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        _logger.LogError(ex, "Failed to cancel expired payment for order code {OrderCode}.", payment.OrderCode);
                        throw;
                    }
                });
            }
        }
    }
}

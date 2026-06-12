using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public sealed class PointsExpiryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PointsExpiryBackgroundService> _logger;

    private static readonly TimeSpan ScheduledTime = new(1, 0, 0);

    public PointsExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PointsExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Points Expiry Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = GetNextDelay();

                _logger.LogInformation("Next points expiry run in {Delay}.", delay);

                await Task.Delay(delay, stoppingToken);

                await ExpirePointsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while expiring points.");
            }
        }

        _logger.LogInformation("Points Expiry Background Service stopped.");
    }

    private static TimeSpan GetNextDelay()
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date + ScheduledTime;

        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun - now;
    }

    private async Task ExpirePointsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var twelveMonthsAgo = now.AddMonths(-12);
        var elevenMonthsAgo = now.AddMonths(-11);

        var accounts = (await unitOfWork.GetRepository<LoyaltyAccount>()
            .GetAllAsync(a => !a.IsDeleted && a.AvailablePoints > 0, cancellationToken))
            .ToList();

        if (accounts.Count == 0)
        {
            _logger.LogInformation("No accounts with points to expire.");
            return;
        }

        var userIds = accounts.Select(a => a.UserId).ToList();
        var orders = await unitOfWork.GetRepository<Order>().GetAllAsync(
            o => userIds.Contains(o.UserId) && o.Status == OrderStatus.Delivered && !o.IsDeleted,
            cancellationToken);

        var lastOrderDates = orders
            .GroupBy(o => o.UserId)
            .ToDictionary(g => g.Key, g => g.Max(o => o.CreatedAt));

        var accountIds = accounts.Select(a => a.Id).ToList();
        var existingExpiredTxs = await unitOfWork.GetRepository<LoyaltyTransaction>().GetAllAsync(
            t => accountIds.Contains(t.LoyaltyAccountId) && t.Type == LoyaltyTransactionType.Expired,
            cancellationToken);

        var expiredTxMap = existingExpiredTxs
            .GroupBy(t => t.LoyaltyAccountId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var txRepo = unitOfWork.GetRepository<LoyaltyTransaction>();
        var totalExpired = 0;

        foreach (var account in accounts)
        {
            if (!lastOrderDates.TryGetValue(account.UserId, out var lastOrderDate))
                continue;

            if (lastOrderDate >= elevenMonthsAgo)
                continue;

            var existingExpired = expiredTxMap.GetValueOrDefault(account.Id) ?? new List<LoyaltyTransaction>();

            if (lastOrderDate < twelveMonthsAgo)
            {
                if (existingExpired.Any(t => t.Status == LoyaltyTransactionStatus.Completed))
                    continue;

                var pending = existingExpired.FirstOrDefault(t => t.Status == LoyaltyTransactionStatus.Pending);
                if (pending != null)
                {
                    pending.Complete();
                    account.ExpirePoints(pending.Points);
                    totalExpired += pending.Points;
                }
                else
                {
                    var tx = LoyaltyTransaction.CreateExpired(account.Id, account.AvailablePoints);
                    await txRepo.AddAsync(tx, cancellationToken);
                    account.ExpirePoints(account.AvailablePoints);
                    totalExpired += tx.Points;
                }
            }
            else
            {
                var hasRecentWarning = existingExpired.Any(t => t.CreatedAt >= now.AddDays(-30));
                if (hasRecentWarning)
                    continue;

                var pendingTx = LoyaltyTransaction.CreatePendingExpired(account.Id, account.AvailablePoints);
                await txRepo.AddAsync(pendingTx, cancellationToken);

                var expiryDate = lastOrderDate.AddMonths(12);
                await notificationService.SendPointsExpiryWarningAsync(
                    account.UserId, account.AvailablePoints, expiryDate);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully expired {Count} points across all inactive accounts.",
            totalExpired);
    }
}

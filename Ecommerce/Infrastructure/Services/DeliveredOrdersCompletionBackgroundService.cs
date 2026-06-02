using Application.Interfaces.Repositories.Base;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public sealed class DeliveredOrdersCompletionBackgroundService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeliveredOrdersCompletionBackgroundService> _logger;

    private static readonly TimeSpan ScheduledTime =
        new(23, 50, 0);

    public DeliveredOrdersCompletionBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DeliveredOrdersCompletionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Delivered Orders Completion Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = GetNextDelay();

                _logger.LogInformation(
                    "Next completion run in {Delay}.",
                    delay);

                await Task.Delay(delay, stoppingToken);

                await CompleteDeliveredOrdersAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while completing delivered orders.");
            }
        }

        _logger.LogInformation(
            "Delivered Orders Completion Background Service stopped.");
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

    private async Task CompleteDeliveredOrdersAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var unitOfWork =
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var query = unitOfWork
            .GetRepository<Order>()
            .GetQueryable()
            .Where(o =>
                o.Status == OrderStatus.Delivered &&
                !o.IsDeleted);

        var affectedRows = await query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(
                    o => o.Status,
                    OrderStatus.Completed)
                .SetProperty(
                    o => o.UpdatedAt,
                    DateTime.UtcNow),
            cancellationToken);

        _logger.LogInformation(
            "Marked {Count} delivered orders as completed.",
            affectedRows);
    }
}
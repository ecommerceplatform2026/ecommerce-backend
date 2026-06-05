using Application.Interfaces.Services;
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

        var loyaltyService = scope.ServiceProvider.GetRequiredService<ILoyaltyService>();

        var result = await loyaltyService.ExpireInactivePointsAsync(cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Successfully expired {Count} points across all inactive accounts.",
                result.Value);
        }
        else
        {
            _logger.LogWarning(
                "Points expiry completed with errors: {Errors}.",
                string.Join("; ", result.Errors));
        }
    }
}

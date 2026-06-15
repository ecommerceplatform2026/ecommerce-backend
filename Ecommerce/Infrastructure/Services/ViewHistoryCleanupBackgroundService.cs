using Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public sealed class ViewHistoryCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ViewHistoryCleanupBackgroundService> _logger;

        private static readonly TimeSpan ScheduledTime = new(2, 0, 0); // Runs daily at 2:00 AM UTC

        public ViewHistoryCleanupBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ViewHistoryCleanupBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("View History Cleanup Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var delay = GetNextDelay();

                    _logger.LogInformation("Next view history cleanup run in {Delay}.", delay);

                    await Task.Delay(delay, stoppingToken);

                    await CleanupViewHistoryAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // graceful shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while cleaning up view history.");
                }
            }

            _logger.LogInformation("View History Cleanup Background Service stopped.");
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

        private async Task CleanupViewHistoryAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var recommendationService = scope.ServiceProvider.GetRequiredService<IRecommendationService>();

            var result = await recommendationService.CleanupOldViewHistoriesAsync(cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Successfully cleaned up {Count} old view history records.",
                    result.Value);
            }
            else
            {
                _logger.LogWarning(
                    "View history cleanup completed with errors: {Errors}.",
                    string.Join("; ", result.Errors));
            }
        }
    }
}

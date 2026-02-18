using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;

namespace SpeakHalo.Infrastructure.Services;

/// <summary>
/// Background service that processes pending follow-up messages every 2 minutes
/// </summary>
public class FollowUpProcessingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FollowUpProcessingBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(2); // Check every 2 minutes

    public FollowUpProcessingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<FollowUpProcessingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Follow-Up Processing Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessFollowUpsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Follow-Up Processing Background Service");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Follow-Up Processing Background Service stopped");
    }

    private async Task ProcessFollowUpsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var followUpService = scope.ServiceProvider.GetRequiredService<IAIFollowUpService>();

        try
        {
            var processedCount = await followUpService.ProcessPendingFollowUpsAsync();

            if (processedCount > 0)
            {
                _logger.LogInformation("Processed {Count} pending follow-up messages", processedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing follow-ups in background service");
        }
    }
}

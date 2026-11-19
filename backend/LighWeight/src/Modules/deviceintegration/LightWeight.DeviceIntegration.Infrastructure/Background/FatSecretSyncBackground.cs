using System.Threading.Channels;
using LightWeight.DeviceIntegration.Application.Abstractions;
using LightWeight.DeviceIntegration.Application.Services;
using LightWeight.DeviceIntegration.Application.Utils;
using LightWeight.DeviceIntegration.Domain.Repositories;
using LightWeight.shared.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LightWeight.DeviceIntegration.Infrastructure.Background;

/// <summary>
/// In memory: a restart loses what was queued, and that is fine. The nightly run queues every linked user again,
/// and a history import that did not finish is started again (HistoryImportedAt is still null)
/// </summary>
internal sealed class FatSecretSyncQueue : IFatSecretSyncQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(Guid userId) => _channel.Writer.TryWrite(userId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
}

/// <summary>
/// The only consumer of the queue: one user at a time, each in its own scope (its own DbContexts), so the nightly
/// run and an import after linking never read the same user at once. A failing user is logged and the next one runs
/// </summary>
internal sealed class FatSecretSyncWorker(FatSecretSyncQueue queue, IServiceScopeFactory scopeFactory, ILogger<FatSecretSyncWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (Guid userId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<FatSecretDiarySynchronizer>().SynchronizeAsync(userId, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Not retried: the next night reads the same window again
                logger.LogError(exception, "FatSecret synchronization of user {UserId} failed", userId);
            }
        }
    }
}

/// <summary>Every night at 03:30 (Europe/Madrid) queues every linked user</summary>
internal sealed class FatSecretNightlyScheduler(IFatSecretSyncQueue queue, IFatSecretClient fatSecret, IServiceScopeFactory scopeFactory,
    IClock clock, ILogger<FatSecretNightlyScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!fatSecret.IsConfigured)
        {
            logger.LogInformation("FatSecret is not configured: the nightly synchronization is off");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime next = NightlySchedule.NextRunUtc(clock.UtcNow);
            TimeSpan wait = next - clock.UtcNow;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, stoppingToken);

            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                List<Guid> userIds = await scope.ServiceProvider.GetRequiredService<IFatSecretConnectionRepository>()
                    .GetLinkedUserIdsAsync(stoppingToken);
                userIds.ForEach(queue.Enqueue);
                logger.LogInformation("FatSecret nightly synchronization: {Users} users queued", userIds.Count);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "FatSecret nightly synchronization could not start");
            }
        }
    }
}

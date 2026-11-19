using LightWeight.DeviceIntegration.Application.Abstractions;
using LightWeight.DeviceIntegration.Domain.Aggregates;
using LightWeight.DeviceIntegration.Domain.Repositories;
using LightWeight.DeviceIntegration.Domain.Uow;
using LightWeight.shared.Contracts.DeviceIntegration;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;
using Microsoft.Extensions.Logging;

namespace LightWeight.DeviceIntegration.Application.Services;

/// <summary>
/// Reads the FatSecret diary of a user and publishes it as <see cref="FatSecretDiarySyncedIntegrationEvent"/>.
/// <list type="bullet">
/// <item>Just linked (history not imported): the whole diary, month by month backwards, one event per month.</item>
/// <item>Otherwise: from the 1st of the previous month to today, one event. Corrections of past days and a missed
/// night are caught by reading the same window again.</item>
/// </list>
/// Days after today (Europe/Madrid) are left out: FatSecret lets the user plan meals ahead, and they are not eaten yet
/// </summary>
public sealed class FatSecretDiarySynchronizer
{
    /// <summary>The history import stops after this many months in a row without entries</summary>
    public const int EmptyMonthsToStop = 3;
    /// <summary>And never goes further back than this</summary>
    public const int MaxHistoryMonths = 120;

    private readonly IFatSecretConnectionRepository _connections;
    private readonly IDeviceIntegrationUnitOfWork _unitOfWork;
    private readonly IFatSecretClient _fatSecret;
    private readonly IEventPublisher _publisher;
    private readonly IClock _clock;
    private readonly ILogger<FatSecretDiarySynchronizer> _logger;

    public FatSecretDiarySynchronizer(IFatSecretConnectionRepository connections, IDeviceIntegrationUnitOfWork unitOfWork,
        IFatSecretClient fatSecret, IEventPublisher publisher, IClock clock, ILogger<FatSecretDiarySynchronizer> logger)
    {
        _connections = connections;
        _unitOfWork = unitOfWork;
        _fatSecret = fatSecret;
        _publisher = publisher;
        _clock = clock;
        _logger = logger;
    }

    public async Task SynchronizeAsync(Guid userId, CancellationToken ct)
    {
        FatSecretConnection? connection = await _connections.GetByUserIdAsync(userId, ct);
        if (connection is null || !connection.IsLinked)
            return;

        var accessToken = new OAuthToken(connection.AccessToken!, connection.AccessTokenSecret!);
        DateOnly today = UserCalendar.Today(_clock.UtcNow);
        try
        {
            if (connection.HistoryImportedAt is null)
            {
                int months = await ImportHistoryAsync(userId, accessToken, today, ct);
                connection.MarkHistoryImported(_clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(ct);
                _logger.LogInformation("FatSecret history of user {UserId} imported: {Months} months", userId, months);
            }
            else
            {
                await SyncRecentAsync(userId, accessToken, today, ct);
            }
        }
        catch (FatSecretAccessRevokedException)
        {
            // The user has to link the account again; the nightly run skips it until then
            connection.MarkAccessRevoked(_clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogWarning("FatSecret rejected the access token of user {UserId}: marked as revoked", userId);
        }
    }

    private async Task SyncRecentAsync(Guid userId, OAuthToken accessToken, DateOnly today, CancellationToken ct)
    {
        DateOnly from = FirstDayOfMonth(today).AddMonths(-1);
        IReadOnlyList<FatSecretDiaryDay> previousMonth = await _fatSecret.GetMonthAsync(accessToken, from, ct);
        IReadOnlyList<FatSecretDiaryDay> currentMonth = await _fatSecret.GetMonthAsync(accessToken, today, ct);

        await PublishAsync(userId, from, today, [.. previousMonth, .. currentMonth], ct);
    }

    private async Task<int> ImportHistoryAsync(Guid userId, OAuthToken accessToken, DateOnly today, CancellationToken ct)
    {
        DateOnly month = FirstDayOfMonth(today);
        int emptyInARow = 0;
        int read = 0;
        while (emptyInARow < EmptyMonthsToStop && read < MaxHistoryMonths)
        {
            IReadOnlyList<FatSecretDiaryDay> days = await _fatSecret.GetMonthAsync(accessToken, month, ct);
            DateOnly lastDay = month.AddMonths(1).AddDays(-1);
            await PublishAsync(userId, month, lastDay < today ? lastDay : today, days, ct);

            emptyInARow = days.Count == 0 ? emptyInARow + 1 : 0;
            read++;
            month = month.AddMonths(-1);
        }
        return read;
    }

    private async Task PublishAsync(Guid userId, DateOnly from, DateOnly to, IEnumerable<FatSecretDiaryDay> days, CancellationToken ct)
    {
        List<FatSecretDiaryDay> inPeriod = days
            .Where(d => d.Date >= from && d.Date <= to)
            .DistinctBy(d => d.Date)
            .OrderBy(d => d.Date)
            .ToList();
        await _publisher.PublishAsync(new FatSecretDiarySyncedIntegrationEvent(userId, from, to, inPeriod, _clock.UtcNow), ct);
    }

    private static DateOnly FirstDayOfMonth(DateOnly date) => new(date.Year, date.Month, 1);
}

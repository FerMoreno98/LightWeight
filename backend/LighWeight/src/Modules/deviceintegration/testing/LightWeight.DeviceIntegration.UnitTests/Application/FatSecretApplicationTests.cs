using LightWeight.DeviceIntegration.Application.Abstractions;
using LightWeight.DeviceIntegration.Application.Commands.FatSecret;
using LightWeight.DeviceIntegration.Application.Exceptions;
using LightWeight.DeviceIntegration.Application.Queries.FatSecret;
using LightWeight.DeviceIntegration.Application.Services;
using LightWeight.DeviceIntegration.Application.Utils;
using LightWeight.DeviceIntegration.Domain.Aggregates;
using LightWeight.DeviceIntegration.Domain.Exceptions;
using LightWeight.DeviceIntegration.Domain.Repositories;
using LightWeight.DeviceIntegration.Domain.Uow;
using LightWeight.shared.Contracts.DeviceIntegration;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LightWeight.DeviceIntegration.UnitTests.Application;

public class FatSecretApplicationTests
{
    // 19/11/2026 08:00 UTC = 09:00 in Madrid
    private static readonly DateTime Now = new(2026, 11, 19, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 11, 19);
    private static readonly Guid UserId = Guid.CreateVersion7();

    private readonly IFatSecretConnectionRepository _connections = Substitute.For<IFatSecretConnectionRepository>();
    private readonly IDeviceIntegrationUnitOfWork _unitOfWork = Substitute.For<IDeviceIntegrationUnitOfWork>();
    private readonly IFatSecretClient _fatSecret = Substitute.For<IFatSecretClient>();
    private readonly IFatSecretSyncQueue _queue = Substitute.For<IFatSecretSyncQueue>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public FatSecretApplicationTests()
    {
        _clock.UtcNow.Returns(Now);
        _fatSecret.IsConfigured.Returns(true);
        _fatSecret.GetMonthAsync(default!, default, default).ReturnsForAnyArgs([]);
    }

    private FatSecretConnection Linked(bool historyImported = true)
    {
        FatSecretConnection connection = FatSecretConnection.Create(UserId);
        connection.StartLinking("request", "request-secret", Now);
        connection.CompleteLinking("request", "access", "access-secret", Now);
        if (historyImported)
            connection.MarkHistoryImported(Now);
        _connections.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(connection);
        return connection;
    }

    private FatSecretDiarySynchronizer Synchronizer()
        => new(_connections, _unitOfWork, _fatSecret, _publisher, _clock, NullLogger<FatSecretDiarySynchronizer>.Instance);

    private static FatSecretDiaryDay Day(DateOnly date, decimal calories = 2000m) => new(date, calories, 150m, 250m, 70m);

    // ---------------------------------------------------------------- Linking

    [Fact]
    public async Task StartLinking_FirstTime_CreatesTheConnectionAndReturnsTheAuthorizePage()
    {
        _fatSecret.GetRequestTokenAsync(Arg.Any<CancellationToken>()).Returns(new OAuthToken("request", "request-secret"));
        _fatSecret.GetAuthorizeUrl("request").Returns("https://fatsecret/authorize?oauth_token=request");
        var handler = new StartFatSecretLinkingCommandHandler(_connections, _unitOfWork, _fatSecret, _clock);

        string url = await handler.HandleAsync(new StartFatSecretLinkingCommand(UserId));

        Assert.Equal("https://fatsecret/authorize?oauth_token=request", url);
        await _connections.Received(1).AddAsync(
            Arg.Is<FatSecretConnection>(c => c.UserId == UserId && c.RequestToken == "request" && c.RequestTokenSecret == "request-secret"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartLinking_WithoutConfiguration_Throws()
    {
        _fatSecret.IsConfigured.Returns(false);
        var handler = new StartFatSecretLinkingCommandHandler(_connections, _unitOfWork, _fatSecret, _clock);

        await Assert.ThrowsAsync<FatSecretNotConfiguredApplicationException>(() => handler.HandleAsync(new StartFatSecretLinkingCommand(UserId)));
        await _fatSecret.DidNotReceive().GetRequestTokenAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteLinking_ExchangesTheTokenSavesAndQueuesTheImport()
    {
        FatSecretConnection connection = FatSecretConnection.Create(UserId);
        connection.StartLinking("request", "request-secret", Now);
        _connections.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(connection);
        _fatSecret.GetAccessTokenAsync(new OAuthToken("request", "request-secret"), "verifier", Arg.Any<CancellationToken>())
            .Returns(new OAuthToken("access", "access-secret"));
        var handler = new CompleteFatSecretLinkingCommandHandler(_connections, _unitOfWork, _fatSecret, _queue, _clock);

        await handler.HandleAsync(new CompleteFatSecretLinkingCommand(UserId, "request", "verifier"));

        Assert.True(connection.IsLinked);
        Assert.Equal("access", connection.AccessToken);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _queue.Received(1).Enqueue(UserId);
    }

    [Fact]
    public async Task CompleteLinking_WithTheTokenOfAnotherRequest_DoesNotCallFatSecret()
    {
        FatSecretConnection connection = FatSecretConnection.Create(UserId);
        connection.StartLinking("request", "request-secret", Now);
        _connections.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(connection);
        var handler = new CompleteFatSecretLinkingCommandHandler(_connections, _unitOfWork, _fatSecret, _queue, _clock);

        await Assert.ThrowsAsync<LinkingRequestMismatchDomainException>(() =>
            handler.HandleAsync(new CompleteFatSecretLinkingCommand(UserId, "someone-else", "verifier")));

        await _fatSecret.DidNotReceiveWithAnyArgs().GetAccessTokenAsync(default!, default!, default);
        _queue.DidNotReceiveWithAnyArgs().Enqueue(default);
    }

    [Fact]
    public async Task Unlink_RemovesTheConnection()
    {
        FatSecretConnection connection = Linked();
        var handler = new UnlinkFatSecretCommandHandler(_connections, _unitOfWork);

        await handler.HandleAsync(new UnlinkFatSecretCommand(UserId));

        _connections.Received(1).Remove(connection);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("none", "NotLinked")]
    [InlineData("pending", "NotLinked")]
    [InlineData("linked", "Linked")]
    [InlineData("revoked", "Revoked")]
    public async Task GetConnection_ReturnsTheStatus(string state, string expected)
    {
        if (state != "none")
        {
            FatSecretConnection connection = FatSecretConnection.Create(UserId);
            connection.StartLinking("request", "request-secret", Now);
            if (state is "linked" or "revoked")
                connection.CompleteLinking("request", "access", "access-secret", Now);
            if (state == "revoked")
                connection.MarkAccessRevoked(Now);
            _connections.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(connection);
        }
        var handler = new GetFatSecretConnectionQueryHandler(_connections, _fatSecret);

        FatSecretConnectionResponse response = await handler.HandleAsync(new GetFatSecretConnectionQuery(UserId));

        Assert.True(response.Available);
        Assert.Equal(expected, response.Status);
    }

    // ---------------------------------------------------------------- Synchronization

    [Fact]
    public async Task Synchronize_Recent_ReadsTwoMonthsAndPublishesFromThe1stOfThePreviousMonthToToday()
    {
        Linked();
        _fatSecret.GetMonthAsync(Arg.Any<OAuthToken>(), new DateOnly(2026, 10, 1), Arg.Any<CancellationToken>())
            .Returns([Day(new DateOnly(2026, 10, 31))]);
        _fatSecret.GetMonthAsync(Arg.Any<OAuthToken>(), Today, Arg.Any<CancellationToken>())
            // Tomorrow planned in FatSecret: not eaten yet, left out
            .Returns([Day(Today.AddDays(-1)), Day(Today), Day(Today.AddDays(1))]);

        await Synchronizer().SynchronizeAsync(UserId, default);

        await _publisher.Received(1).PublishAsync(
            Arg.Is<FatSecretDiarySyncedIntegrationEvent>(e =>
                e.UserId == UserId &&
                e.From == new DateOnly(2026, 10, 1) &&
                e.To == Today &&
                e.Days.Select(d => d.Date).SequenceEqual(new[] { new DateOnly(2026, 10, 31), Today.AddDays(-1), Today })),
            Arg.Any<CancellationToken>());
        await _fatSecret.Received(2).GetMonthAsync(Arg.Any<OAuthToken>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Synchronize_JustLinked_ImportsMonthByMonthUntil3EmptyMonthsInARow()
    {
        FatSecretConnection connection = Linked(historyImported: false);
        // November and October have entries; September, August and July are empty -> stops
        _fatSecret.GetMonthAsync(Arg.Any<OAuthToken>(), new DateOnly(2026, 11, 1), Arg.Any<CancellationToken>()).Returns([Day(Today)]);
        _fatSecret.GetMonthAsync(Arg.Any<OAuthToken>(), new DateOnly(2026, 10, 1), Arg.Any<CancellationToken>()).Returns([Day(new DateOnly(2026, 10, 2))]);

        await Synchronizer().SynchronizeAsync(UserId, default);

        await _fatSecret.Received(5).GetMonthAsync(Arg.Any<OAuthToken>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
        // The current month ends today; the others at their last day
        await _publisher.Received(1).PublishAsync(
            Arg.Is<FatSecretDiarySyncedIntegrationEvent>(e => e.From == new DateOnly(2026, 11, 1) && e.To == Today && e.Days.Count == 1),
            Arg.Any<CancellationToken>());
        await _publisher.Received(1).PublishAsync(
            Arg.Is<FatSecretDiarySyncedIntegrationEvent>(e => e.From == new DateOnly(2026, 7, 1) && e.To == new DateOnly(2026, 7, 31) && e.Days.Count == 0),
            Arg.Any<CancellationToken>());
        Assert.Equal(Now, connection.HistoryImportedAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Synchronize_WhenFatSecretRejectsTheToken_MarksItRevoked()
    {
        FatSecretConnection connection = Linked();
        _fatSecret.GetMonthAsync(default!, default, default).ThrowsAsyncForAnyArgs(new FatSecretAccessRevokedException());

        await Synchronizer().SynchronizeAsync(UserId, default);

        Assert.False(connection.IsLinked);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceiveWithAnyArgs().PublishAsync<FatSecretDiarySyncedIntegrationEvent>(default!, default);
    }

    [Fact]
    public async Task Synchronize_WhenRevoked_DoesNothing()
    {
        Linked().MarkAccessRevoked(Now);

        await Synchronizer().SynchronizeAsync(UserId, default);

        await _fatSecret.DidNotReceiveWithAnyArgs().GetMonthAsync(default!, default, default);
    }

    [Fact]
    public async Task Synchronize_OnATransientError_KeepsTheHistoryPending()
    {
        FatSecretConnection connection = Linked(historyImported: false);
        _fatSecret.GetMonthAsync(default!, default, default).ThrowsAsyncForAnyArgs(new HttpRequestException("down"));

        await Assert.ThrowsAsync<HttpRequestException>(() => Synchronizer().SynchronizeAsync(UserId, default));

        Assert.Null(connection.HistoryImportedAt);
        Assert.True(connection.IsLinked);
    }

    // ---------------------------------------------------------------- Nightly schedule

    [Theory]
    [InlineData("2026-11-19T01:00:00Z", "2026-11-19T02:30:00Z")] // winter (UTC+1), before the run
    [InlineData("2026-11-19T02:30:00Z", "2026-11-20T02:30:00Z")] // exactly at the run: the next one
    [InlineData("2026-07-01T12:00:00Z", "2026-07-02T01:30:00Z")] // summer (UTC+2)
    [InlineData("2026-03-28T12:00:00Z", "2026-03-29T01:30:00Z")] // the night the clock goes forward
    [InlineData("2026-10-24T12:00:00Z", "2026-10-25T02:30:00Z")] // the night the clock goes back
    public void NightlySchedule_NextRun_Is0330InMadrid(string now, string expected)
    {
        DateTime utcNow = DateTime.Parse(now, null, System.Globalization.DateTimeStyles.AdjustToUniversal);

        DateTime next = NightlySchedule.NextRunUtc(utcNow);

        Assert.Equal(DateTime.Parse(expected, null, System.Globalization.DateTimeStyles.AdjustToUniversal), next);
    }
}

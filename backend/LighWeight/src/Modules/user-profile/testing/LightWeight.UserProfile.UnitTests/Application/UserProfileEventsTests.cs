using LightWeight.shared.Contracts.Dashboard;
using LightWeight.shared.Contracts.UserProfile;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;
using LightWeight.UserProfile.Application.Events;
using LightWeight.UserProfile.Domain.Aggregates;
using LightWeight.UserProfile.Domain.Enum;
using LightWeight.UserProfile.Domain.Events;
using LightWeight.UserProfile.Domain.Repository;
using NSubstitute;
using Xunit;

namespace LightWeight.UserProfile.UnitTests.Application;

public class UserProfileEventsTests
{
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    public UserProfileEventsTests()
    {
        _clock.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task Completed_PublishesProfileRecordedWithTheSex()
    {
        var handler = new UserProfileDomainEventsHandler(_publisher);

        await handler.HandleAsync(new UserCompletedDomainEvent(UserId, Sex.Female, Now));

        await _publisher.Received(1).PublishAsync(
            new ProfileRecordedIntegrationEvent(UserId, "Female", Now), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Modified_PublishesProfileRecordedWithTheSex()
    {
        var handler = new UserProfileDomainEventsHandler(_publisher);

        await handler.HandleAsync(new UserProfileModifiedDomainEvent(UserId, Sex.Male, Now));

        await _publisher.Received(1).PublishAsync(
            new ProfileRecordedIntegrationEvent(UserId, "Male", Now), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rebuild_PublishesTheCurrentProfile()
    {
        var user = User.Create(UserId, "Fernando", Sex.Female, new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc), Now.AddYears(-1));
        _users.FindByIdAsync(UserId).Returns(user);
        var handler = new RebuildRequestedIntegrationEventHandler(_users, _publisher, _clock);

        await handler.HandleAsync(new RebuildRequestedIntegrationEvent(UserId, Now));

        await _publisher.Received(1).PublishAsync(
            new ProfileRecordedIntegrationEvent(UserId, "Female", Now), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rebuild_WithoutProfile_PublishesNothing()
    {
        var handler = new RebuildRequestedIntegrationEventHandler(_users, _publisher, _clock);

        await handler.HandleAsync(new RebuildRequestedIntegrationEvent(UserId, Now));

        await _publisher.DidNotReceiveWithAnyArgs().PublishAsync<ProfileRecordedIntegrationEvent>(default!, default);
    }
}

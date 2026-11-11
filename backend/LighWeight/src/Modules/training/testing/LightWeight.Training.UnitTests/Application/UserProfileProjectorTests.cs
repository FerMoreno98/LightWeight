using LightWeight.shared.Contracts.UserProfile;
using LightWeight.Training.Application.Events;
using LightWeight.Training.Application.Projections.UserNames;
using NSubstitute;

namespace LightWeight.Training.UnitTests.Application;

public class UserProfileProjectorTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 9, 0, 0, DateTimeKind.Utc);

    private readonly IUserNameProjectionStore _store = Substitute.For<IUserNameProjectionStore>();
    private readonly Guid _userId = Guid.CreateVersion7();

    [Fact]
    public async Task ProfileRecorded_UpsertsTheNameOfTheUser()
    {
        // Arrange
        var projector = new UserProfileProjector(_store);
        // Act
        await projector.HandleAsync(new ProfileRecordedIntegrationEvent(_userId, "Fernando", "Male", Now));
        // Assert
        await _store.Received(1).UpsertAsync(
            Arg.Is<UserNameProjection>(p => p.UserId == _userId && p.Name == "Fernando"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProfileRecorded_Again_UpsertsTheNewName()
    {
        // Arrange
        var projector = new UserProfileProjector(_store);
        // Act
        await projector.HandleAsync(new ProfileRecordedIntegrationEvent(_userId, "Fernando", "Male", Now));
        await projector.HandleAsync(new ProfileRecordedIntegrationEvent(_userId, "Fer", "Male", Now.AddDays(1)));
        // Assert
        await _store.Received(1).UpsertAsync(
            Arg.Is<UserNameProjection>(p => p.UserId == _userId && p.Name == "Fer"),
            Arg.Any<CancellationToken>());
    }
}

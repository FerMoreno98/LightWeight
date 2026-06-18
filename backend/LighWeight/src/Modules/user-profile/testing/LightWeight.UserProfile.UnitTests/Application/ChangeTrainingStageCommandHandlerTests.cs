
using LightWeight.UserProfile.Application.Commands.ChangeTrainingStage;
using LightWeight.UserProfile.Application.Exceptions;
using LightWeight.UserProfile.Domain.Aggregates;
using LightWeight.UserProfile.Domain.Enum;
using LightWeight.UserProfile.Domain.Repository;
using LightWeight.UserProfile.Domain.Uow;
using NSubstitute;
using Xunit;

namespace LightWeight.UserProfile.UnitTests.Application;

public class ChangeTrainingStageCommandHandlerTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────────

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserProfileUnitOfWork     _uow            = Substitute.For<IUserProfileUnitOfWork>();

    private static readonly DateTime FixedNow =
        new(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private ChangeTrainingStageCommandHandler BuildHandler() =>
        new(_userRepository, _uow);

    private static User BuildUser(TrainingStage stage = TrainingStage.Bulk) =>
        User.Create(
            Guid.NewGuid(),
            "Fernando",
            Sex.Male,
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            stage,
            FixedNow);

    private static ChangeTrainingStageCommand BuildCommand(
        Guid?  userId = null,
        string stage  = "Cut") =>
        new(userId ?? Guid.NewGuid(), stage);

    // ─── Usuario no encontrado ───────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        _userRepository.FindByIdAsync(Arg.Any<Guid>()).Returns((User?)null);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => handler.HandleAsync(BuildCommand()));
    }

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_DoesNotCallSaveChanges()
    {
        _userRepository.FindByIdAsync(Arg.Any<Guid>()).Returns((User?)null);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => handler.HandleAsync(BuildCommand()));

        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ChangesUserStage()
    {
        var user = BuildUser(TrainingStage.Bulk);
        _userRepository.FindByIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(userId: user.Id, stage: "Cut"));

        Assert.Equal(TrainingStage.Cut, user.CurrentStage);
    }

    [Fact]
    public async Task HandleAsync_CallsSaveChanges()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(userId: user.Id));

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── Enum parsing ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Bulk",        TrainingStage.Bulk)]
    [InlineData("Cut",         TrainingStage.Cut)]
    [InlineData("Maintenance", TrainingStage.Maintenance)]
    [InlineData("bulk",        TrainingStage.Bulk)]
    [InlineData("CUT",         TrainingStage.Cut)]
    public async Task HandleAsync_ParsesStageCaseInsensitive(string input, TrainingStage expected)
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(userId: user.Id, stage: input));

        Assert.Equal(expected, user.CurrentStage);
    }

    // ─── CancellationToken ───────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_PropagatesCancellationTokenToSaveChanges()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(user.Id).Returns(user);
        var cts     = new CancellationTokenSource();
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(userId: user.Id), cts.Token);

        await _uow.Received(1).SaveChangesAsync(cts.Token);
    }
}

using LightWeight.UserProfile.Application.Commands.UpdateProfile;
using LightWeight.UserProfile.Application.Exceptions;
using LightWeight.UserProfile.Domain.Aggregates;
using LightWeight.UserProfile.Domain.Enum;
using LightWeight.UserProfile.Domain.Repository;
using LightWeight.UserProfile.Domain.Uow;
using NSubstitute;
using Xunit;

namespace LightWeight.UserProfile.UnitTests.Application;

public class UpdateProfileCommandHandlerTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────────

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserProfileUnitOfWork     _uow            = Substitute.For<IUserProfileUnitOfWork>();

    private static readonly DateTime FixedNow =
        new(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private UpdateProfileCommandHandler BuildHandler() =>
        new(_userRepository, _uow);

    private static User BuildUser() =>
        User.Create(
            Guid.NewGuid(),
            "Fernando",
            Sex.Male,
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            TrainingStage.Bulk,
            FixedNow);

    private static UpdateProfileCommand BuildCommand(
        Guid?     userId      = null,
        string    name        = "Carlos",
        string    sex         = "Male",
        DateTime? dateOfBirth = null) =>
        new(
            UserId:      userId      ?? Guid.NewGuid(),
            Name:        name,
            Sex:         sex,
            DateOfBirth: dateOfBirth ?? new DateTime(1995, 3, 20, 0, 0, 0, DateTimeKind.Utc));

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
    public async Task HandleAsync_UpdatesName()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(userId: user.Id, name: "Carlos"));

        Assert.Equal("Carlos", user.Name);
    }

    [Fact]
    public async Task HandleAsync_UpdatesSex()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(userId: user.Id, sex: "Female"));

        Assert.Equal(Sex.Female, user.Sex);
    }

    [Fact]
    public async Task HandleAsync_UpdatesDateOfBirth()
    {
        var user   = BuildUser();
        var newDob = new DateTime(1995, 3, 20, 0, 0, 0, DateTimeKind.Utc);
        _userRepository.FindByIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(userId: user.Id, dateOfBirth: newDob));

        Assert.Equal(newDob, user.DateOfBirth);
    }

    [Fact]
    public async Task HandleAsync_DoesNotChangeCurrentStage()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(userId: user.Id));

        Assert.Equal(TrainingStage.Bulk, user.CurrentStage);
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
    [InlineData("Male",   Sex.Male)]
    [InlineData("Female", Sex.Female)]
    [InlineData("male",   Sex.Male)]
    [InlineData("FEMALE", Sex.Female)]
    public async Task HandleAsync_ParsesSexCaseInsensitive(string input, Sex expected)
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(userId: user.Id, sex: input));

        Assert.Equal(expected, user.Sex);
    }

    // ─── CancellationToken ───────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_PropagatesCancellationTokenToSaveChanges()
    {
        var user    = BuildUser();
        var cts     = new CancellationTokenSource();
        _userRepository.FindByIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(userId: user.Id), cts.Token);

        await _uow.Received(1).SaveChangesAsync(cts.Token);
    }
}
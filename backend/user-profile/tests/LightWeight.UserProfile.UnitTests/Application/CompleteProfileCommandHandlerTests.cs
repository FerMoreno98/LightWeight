using LightWeight.shared.BuildingBlocks.Persistance;
using LightWeight.shared.Types;
using LightWeight.UserProfile.Application.Commands.CompleteProfile;
using LightWeight.UserProfile.Domain.Aggregates;
using LightWeight.UserProfile.Domain.Enum;
using LightWeight.UserProfile.Domain.Repository;
using NSubstitute;
using Xunit;

namespace LightWeight.UserProfile.UnitTests.Application;

public class CompleteProfileCommandHandlerTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────────

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork     _uow            = Substitute.For<IUnitOfWork>();
    private readonly IClock          _clock          = Substitute.For<IClock>();

    private static readonly DateTime FixedNow =
        new(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private static readonly Guid FixedUserId = Guid.NewGuid();

    private CompleteProfileCommandHandler BuildHandler() =>
        new(_uow, _userRepository, _clock);

    private static CompleteProfileCommand BuildCommand(
        Guid?    userId       = null,
        string   name         = "Fernando",
        DateTime? dateOfBirth = null,
        string   sex          = "Male",
        string   stage        = "Bulk") =>
        new(
            UserId:       userId      ?? FixedUserId,
            Name:         name,
            DateOfBirth:  dateOfBirth ?? new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Sex:          sex,
            CurrentStage: stage);

    public CompleteProfileCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    // ─── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_CallsAddAsync()
    {
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        await _userRepository.Received(1)
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AddsUserWithCorrectId()
    {
        User? captured = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>());
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(userId: FixedUserId));

        Assert.Equal(FixedUserId, captured!.Id);
    }

    [Fact]
    public async Task HandleAsync_AddsUserWithCorrectName()
    {
        User? captured = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>());
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(name: "Fernando"));

        Assert.Equal("Fernando", captured!.Name);
    }

    [Fact]
    public async Task HandleAsync_AddsUserWithCorrectSex()
    {
        User? captured = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>());
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(sex: "Female"));

        Assert.Equal(Sex.Female, captured!.Sex);
    }

    [Fact]
    public async Task HandleAsync_AddsUserWithCorrectDateOfBirth()
    {
        var dob = new DateTime(1995, 3, 20, 0, 0, 0, DateTimeKind.Utc);
        User? captured = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>());
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(dateOfBirth: dob));

        Assert.Equal(dob, captured!.DateOfBirth);
    }

    [Fact]
    public async Task HandleAsync_AddsUserWithCorrectTrainingStage()
    {
        User? captured = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>());
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(stage: "Cut"));

        Assert.Equal(TrainingStage.Cut, captured!.CurrentStage);
    }

    [Fact]
    public async Task HandleAsync_AddsUserWithStageStartedAtFromClock()
    {
        User? captured = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>());
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        Assert.Equal(FixedNow, captured!.StageStartedAt);
    }

    [Fact]
    public async Task HandleAsync_CallsSaveChanges()
    {
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SaveChanges_IsCalledAfterAddAsync()
    {
        var callLog = new List<string>();
        _userRepository
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(_ => { callLog.Add("add"); return Task.CompletedTask; });
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => { callLog.Add("save"); return Task.CompletedTask; });
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        Assert.Equal(new[] { "add", "save" }, callLog);
    }

    // ─── Enum parsing ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Male",   Sex.Male)]
    [InlineData("Female", Sex.Female)]
    [InlineData("male",   Sex.Male)]
    [InlineData("FEMALE", Sex.Female)]
    public async Task HandleAsync_ParsesSexCaseInsensitive(string input, Sex expected)
    {
        User? captured = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>());
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(sex: input));

        Assert.Equal(expected, captured!.Sex);
    }

    [Theory]
    [InlineData("Bulk",        TrainingStage.Bulk)]
    [InlineData("Cut",         TrainingStage.Cut)]
    [InlineData("Maintenance", TrainingStage.Maintenance)]
    [InlineData("bulk",        TrainingStage.Bulk)]
    [InlineData("CUT",         TrainingStage.Cut)]
    public async Task HandleAsync_ParsesTrainingStageCaseInsensitive(string input, TrainingStage expected)
    {
        User? captured = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>());
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(stage: input));

        Assert.Equal(expected, captured!.CurrentStage);
    }

    // ─── CancellationToken ───────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_PropagatesCancellationTokenToAddAsync()
    {
        var cts     = new CancellationTokenSource();
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(), cts.Token);

        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), cts.Token);
    }

    [Fact]
    public async Task HandleAsync_PropagatesCancellationTokenToSaveChanges()
    {
        var cts     = new CancellationTokenSource();
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(), cts.Token);

        await _uow.Received(1).SaveChangesAsync(cts.Token);
    }

    // ─── Reloj ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_UsesClockUtcNow()
    {
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        _ = _clock.Received().UtcNow;
    }
}
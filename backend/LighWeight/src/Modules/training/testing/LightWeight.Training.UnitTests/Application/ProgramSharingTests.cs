using LightWeight.Training.Application.Commands.Programs.CopySharedProgram;
using LightWeight.Training.Application.Commands.Programs.RevokeProgramShare;
using LightWeight.Training.Application.Commands.Programs.ShareProgram;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Projections.UserNames;
using LightWeight.Training.Application.Queries.Programs.GetSharedProgram;
using LightWeight.Training.Application.Queries.Programs.GetUserPrograms;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Domain.ValueObjects;
using NSubstitute;

namespace LightWeight.Training.UnitTests.Application;

public class ProgramSharingTests
{
    private readonly IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
    private readonly IExerciseRepository _exerciseRepository = Substitute.For<IExerciseRepository>();
    private readonly IUserNameProjectionStore _userNames = Substitute.For<IUserNameProjectionStore>();
    private readonly ITrainingUnitOfWork _uow = Substitute.For<ITrainingUnitOfWork>();

    private readonly Guid _authorId = Guid.CreateVersion7();
    private readonly Guid _receiverId = Guid.CreateVersion7();

    private Program CreateSharedProgram(Exercise exercise)
    {
        Program program = Program.Create(_authorId, Periodization.Linear, "Push Pull Legs", new List<MuscleGroups> { MuscleGroups.Chest });
        TrainingTemplate template = TrainingTemplate.Create("Hipertrofia base", VolumeLandmarks.MAV, TrainingDistribution.UpperLower, 7);
        TemplateSession session = TemplateSession.Create("Upper A");
        session.AddSet(TemplateSet.Create
        (
            exercise.Id,
            RepetitionRange.Create(6, 10),
            8,
            new List<MuscleGroups> { MuscleGroups.Chest },
            AdvanceTrainingTechniques.Create(true, false, false)
        ));
        template.AddSessionTemplate(session);
        program.AddTrainingTemplate(template);
        program.Share();
        _programRepository.GetByShareTokenAsync(program.ShareToken!).Returns(program);
        return program;
    }

    private static Exercise Bench() =>
        Exercise.Create("Press banca", true, new List<MuscleGroups> { MuscleGroups.Chest });

    // ---------------------------------------------------------------- Share / Revoke

    [Fact]
    public async Task ShareProgram_OfTheUser_ReturnsTheTokenAndSaves()
    {
        // Arrange
        Program program = Program.Create(_authorId, Periodization.Linear, "Programa", new List<MuscleGroups>());
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        var handler = new ShareProgramCommandHandler(_programRepository, _uow);
        // Act
        string token = await handler.HandleAsync(new ShareProgramCommand(program.Id, _authorId));
        // Assert
        Assert.Equal(program.ShareToken, token);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShareProgram_OfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        Program program = Program.Create(_authorId, Periodization.Linear, "Programa", new List<MuscleGroups>());
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        var handler = new ShareProgramCommandHandler(_programRepository, _uow);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.HandleAsync(new ShareProgramCommand(program.Id, _receiverId)));
        Assert.Null(program.ShareToken);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShareProgram_NonExisting_ThrowsNotFound()
    {
        // Arrange
        var handler = new ShareProgramCommandHandler(_programRepository, _uow);
        // Act
        // Assert
        await Assert.ThrowsAsync<ProgramNotFoundApplicationException>(() => handler.HandleAsync(new ShareProgramCommand(Guid.CreateVersion7(), _authorId)));
    }

    [Fact]
    public async Task RevokeProgramShare_OfTheUser_RemovesTheTokenAndSaves()
    {
        // Arrange
        Program program = Program.Create(_authorId, Periodization.Linear, "Programa", new List<MuscleGroups>());
        program.Share();
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        var handler = new RevokeProgramShareCommandHandler(_programRepository, _uow);
        // Act
        await handler.HandleAsync(new RevokeProgramShareCommand(program.Id, _authorId));
        // Assert
        Assert.Null(program.ShareToken);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeProgramShare_OfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        Program program = Program.Create(_authorId, Periodization.Linear, "Programa", new List<MuscleGroups>());
        program.Share();
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        var handler = new RevokeProgramShareCommandHandler(_programRepository, _uow);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.HandleAsync(new RevokeProgramShareCommand(program.Id, _receiverId)));
        Assert.NotNull(program.ShareToken);
    }

    // ---------------------------------------------------------------- Preview

    [Fact]
    public async Task GetSharedProgram_ReturnsThePlanningWithTheExerciseNamesAndTheAuthor()
    {
        // Arrange
        Exercise bench = Bench();
        Program program = CreateSharedProgram(bench);
        _exerciseRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>()).Returns(new List<Exercise> { bench });
        _userNames.GetNameAsync(_authorId, Arg.Any<CancellationToken>()).Returns("Fernando");
        var handler = new GetSharedProgramQueryHandler(_programRepository, _exerciseRepository, _userNames);
        // Act
        GetSharedProgramResponse response = await handler.HandleAsync(new GetSharedProgramQuery(program.ShareToken!));
        // Assert
        Assert.Equal("Push Pull Legs", response.Name);
        Assert.Equal("Linear", response.Periodization);
        Assert.Equal("Fernando", response.AuthorName);
        GetSharedProgramTemplateResponse template = Assert.Single(response.Templates);
        Assert.Equal("Hipertrofia base", template.Name);
        GetSharedProgramSessionResponse session = Assert.Single(template.Sessions);
        Assert.Equal("Upper A", session.Name);
        GetSharedProgramSetResponse set = Assert.Single(session.Sets);
        Assert.Equal("Press banca", set.ExerciseName);
        Assert.Equal(6, set.RepetitionRangeMin);
        Assert.Equal(10, set.RepetitionRangeMax);
        Assert.NotNull(set.Technique);
    }

    [Fact]
    public async Task GetSharedProgram_WithoutProjectedAuthor_ReturnsANullAuthorName()
    {
        // Arrange
        Program program = CreateSharedProgram(Bench());
        _exerciseRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>()).Returns(new List<Exercise>());
        _userNames.GetNameAsync(_authorId, Arg.Any<CancellationToken>()).Returns((string?)null);
        var handler = new GetSharedProgramQueryHandler(_programRepository, _exerciseRepository, _userNames);
        // Act
        GetSharedProgramResponse response = await handler.HandleAsync(new GetSharedProgramQuery(program.ShareToken!));
        // Assert
        Assert.Null(response.AuthorName);
    }

    [Fact]
    public async Task GetSharedProgram_WithUnknownToken_ThrowsNotFound()
    {
        // Arrange
        var handler = new GetSharedProgramQueryHandler(_programRepository, _exerciseRepository, _userNames);
        // Act
        // Assert
        await Assert.ThrowsAsync<SharedProgramNotFoundApplicationException>(() => handler.HandleAsync(new GetSharedProgramQuery("abcdefghijkl")));
    }

    // ---------------------------------------------------------------- Copy

    [Fact]
    public async Task CopySharedProgram_AddsACopyForTheUserAndSaves()
    {
        // Arrange
        Program original = CreateSharedProgram(Bench());
        var handler = new CopySharedProgramCommandHandler(_programRepository, _uow);
        // Act
        Guid copyId = await handler.HandleAsync(new CopySharedProgramCommand(original.ShareToken!, _receiverId));
        // Assert
        Assert.NotEqual(original.Id, copyId);
        await _programRepository.Received(1).AddAsync(
            Arg.Is<Program>(p => p.Id == copyId
                && p.UserId == _receiverId
                && p.CopiedFromProgramId == original.Id
                && p.CopiedFromUserId == _authorId
                && p.ShareToken == null
                && p.trainingTemplates.Count == 1),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CopySharedProgram_DoesNotConsumeTheToken()
    {
        // Arrange
        Program original = CreateSharedProgram(Bench());
        string token = original.ShareToken!;
        var handler = new CopySharedProgramCommandHandler(_programRepository, _uow);
        // Act
        await handler.HandleAsync(new CopySharedProgramCommand(token, _receiverId));
        await handler.HandleAsync(new CopySharedProgramCommand(token, Guid.CreateVersion7()));
        // Assert
        Assert.Equal(token, original.ShareToken);
        await _programRepository.Received(2).AddAsync(Arg.Any<Program>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CopySharedProgram_WithUnknownToken_ThrowsNotFound()
    {
        // Arrange
        var handler = new CopySharedProgramCommandHandler(_programRepository, _uow);
        // Act
        // Assert
        await Assert.ThrowsAsync<SharedProgramNotFoundApplicationException>(() => handler.HandleAsync(new CopySharedProgramCommand("abcdefghijkl", _receiverId)));
        await _programRepository.DidNotReceive().AddAsync(Arg.Any<Program>(), Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------- GetUserPrograms

    [Fact]
    public async Task GetUserPrograms_ReturnsTheShareTokenAndTheAuthorOfTheCopies()
    {
        // Arrange
        Program shared = Program.Create(_receiverId, Periodization.Linear, "Propio", new List<MuscleGroups>());
        shared.Share();
        Program copy = CreateSharedProgram(Bench()).CopyFor(_receiverId);
        _programRepository.GetAllProgramsOfAUserAsync(_receiverId).Returns(new List<Program> { shared, copy });
        _userNames.GetNamesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string> { [_authorId] = "Fernando" });
        var handler = new GetUserProgramsQueryHandler(_programRepository, _userNames);
        // Act
        List<GetUserProgramsResponse> result = await handler.HandleAsync(new GetUserProgramsQuery(_receiverId));
        // Assert
        GetUserProgramsResponse own = result.Single(r => r.Id == shared.Id);
        Assert.Equal(shared.ShareToken, own.ShareToken);
        Assert.False(own.IsCopy);
        Assert.Null(own.CopiedFromAuthorName);

        GetUserProgramsResponse copied = result.Single(r => r.Id == copy.Id);
        Assert.Null(copied.ShareToken);
        Assert.True(copied.IsCopy);
        Assert.Equal("Fernando", copied.CopiedFromAuthorName);
    }

    [Fact]
    public async Task GetUserPrograms_CopyWithoutProjectedAuthor_IsACopyWithANullAuthorName()
    {
        // Arrange
        Program copy = CreateSharedProgram(Bench()).CopyFor(_receiverId);
        _programRepository.GetAllProgramsOfAUserAsync(_receiverId).Returns(new List<Program> { copy });
        _userNames.GetNamesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, string>());
        var handler = new GetUserProgramsQueryHandler(_programRepository, _userNames);
        // Act
        List<GetUserProgramsResponse> result = await handler.HandleAsync(new GetUserProgramsQuery(_receiverId));
        // Assert
        GetUserProgramsResponse copied = Assert.Single(result);
        Assert.True(copied.IsCopy);
        Assert.Null(copied.CopiedFromAuthorName);
    }
}

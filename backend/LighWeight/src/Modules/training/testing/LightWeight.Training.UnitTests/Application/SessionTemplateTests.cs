using LightWeight.Training.Application.Queries.SessionTemplates.GetTemplateSessionsFromTrainingTemplate;
using LightWeight.Training.Application.Commands.TemplateSessions.CreateTemplateSession;
using LightWeight.Training.Application.Commands.TemplateSessions.DeleteTemplateSession;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Queries.SessionTemplates.GetNumberOfSeriesPerGroupPerSession;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Domain.ValueObjects;
using NSubstitute;

namespace LightWeight.Training.UnitTests.Application;

public class SessionTemplateTests
{
    private static (Program program, TrainingTemplate template) CreateProgramWithTemplate(Guid userId)
    {
        Program program = Program.Create
        (
            userId,
            Periodization.Linear,
            "ValidProgramName",
            new List<MuscleGroups>()
        );
        TrainingTemplate trainingTemplate = TrainingTemplate.Create
        (
            "ValidTemplateName",
            VolumeLandmarks.MAV,
            TrainingDistribution.FullBody,
            7
        );
        program.AddTrainingTemplate(trainingTemplate);
        return (program, trainingTemplate);
    }

    [Theory]
    [InlineData("validName")]
    [InlineData("12345")]
    [InlineData("__??")]
    public async Task CreateTemplateSession_WhenHappyPath_ReturnsTemplateId(string name)
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        var (program, trainingTemplate) = CreateProgramWithTemplate(userId);
        _programRepository
            .GetByTrainingTemplateIdAsync(trainingTemplate.Id)
                .Returns(program);
        CreateTemplateSessionCommand command = new CreateTemplateSessionCommand
        (
            trainingTemplate.Id,
            userId,
            name
        );
        CreateTemplateSessionCommandHandler commandHandler = new CreateTemplateSessionCommandHandler
        (
            _programRepository,
            _UOW
        );
        // Act
        Guid TemplateId = await commandHandler.HandleAsync(command,default);
        // Assert
        Assert.NotEqual(TemplateId,Guid.Empty);
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Contains(trainingTemplate.TemplateSessions, s => s.Name == name);
    }

    [Fact]
    public async Task CreateTemplateSessionCommand_WhenTrainingTemplateDoesNotExists_ReturnsApplicationException()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var templateId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        _programRepository
            .GetByTrainingTemplateIdAsync(templateId)
                .Returns((Program?) null);
        CreateTemplateSessionCommand command =
            new CreateTemplateSessionCommand(templateId,userId,"validName");
        CreateTemplateSessionCommandHandler commandHandler =
             new CreateTemplateSessionCommandHandler(_programRepository,_UOW);
        // Act

        // Assert
        await Assert.ThrowsAsync<TrainingTemplateNotFoundApplicationException>
        (
             () =>
                commandHandler.HandleAsync(command, default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTemplateSessionCommand_WhenProgramUserIdDoesNotMatchWithUserId_ReturnsApplicationException()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        var (program, trainingTemplate) = CreateProgramWithTemplate(Guid.CreateVersion7());
        _programRepository
            .GetByTrainingTemplateIdAsync(trainingTemplate.Id)
                .Returns(program);
        CreateTemplateSessionCommand command =
            new CreateTemplateSessionCommand(trainingTemplate.Id,userId,"validName");
        CreateTemplateSessionCommandHandler commandHandler =
             new CreateTemplateSessionCommandHandler(_programRepository,_UOW);
        // Act

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
             () =>
                commandHandler.HandleAsync(command, default)
        );
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTemplateSessionCommand_WhenHappyPath_SoftDeletesTheSession()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        var (program, trainingTemplate) = CreateProgramWithTemplate(userId);
        TemplateSession templateSession = TemplateSession.Create("ValidName");
        trainingTemplate.AddSessionTemplate(templateSession);
        _programRepository
            .GetByTemplateSessionIdAsync(templateSession.Id)
                .Returns(program);
        DeleteTemplateSessionCommandHandler commandHandler =
            new DeleteTemplateSessionCommandHandler(_programRepository,_UOW);
        // Act
        await commandHandler.HandleAsync(new DeleteTemplateSessionCommand(templateSession.Id,userId),default);
        // Assert
        Assert.True(templateSession.IsDeleted);
        Assert.Empty(trainingTemplate.TemplateSessions);
        await _UOW.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTemplateSessionCommand_WhenProgramUserIdDoesNotMatchWithUserId_ReturnsApplicationException()
    {
        // Arrange
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _UOW = Substitute.For<ITrainingUnitOfWork>();
        var (program, trainingTemplate) = CreateProgramWithTemplate(Guid.CreateVersion7());
        TemplateSession templateSession = TemplateSession.Create("ValidName");
        trainingTemplate.AddSessionTemplate(templateSession);
        _programRepository
            .GetByTemplateSessionIdAsync(templateSession.Id)
                .Returns(program);
        DeleteTemplateSessionCommandHandler commandHandler =
            new DeleteTemplateSessionCommandHandler(_programRepository,_UOW);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => commandHandler.HandleAsync(new DeleteTemplateSessionCommand(templateSession.Id,Guid.CreateVersion7()),default)
        );
        Assert.False(templateSession.IsDeleted);
        await _UOW.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetNumberOfSeriesPerGroupPerSession_ReturnSeriesSuccessfully()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        var (program, trainingTemplate) = CreateProgramWithTemplate(userId);
        TemplateSession templateSession = TemplateSession.Create
        (
            "ValidName"
        );
        List<MuscleGroups> muscleGroups = new List<MuscleGroups>
        {
            MuscleGroups.Back,
            MuscleGroups.Biceps,
            MuscleGroups.Biceps,
            MuscleGroups.Chest
        };

        Exercise exercise = Exercise.Create
        (
            "exercis1",
            true,
            muscleGroups
        );
        Exercise exercise2 = Exercise.Create
        (
            "exercis2",
            true,
            new List<MuscleGroups>()
        );
        Exercise exercise3 = Exercise.Create
        (
            "exercis3",
            true,
            muscleGroups
        );
        AdvanceTrainingTechniques advanceTrainingTechniques = AdvanceTrainingTechniques.Create(false,false,false);
        RepetitionRange repetitionRange = RepetitionRange.Create(6,8);
        TemplateSet set1 = TemplateSet.Create
        (
            exercise.Id,
            repetitionRange,
            8,
            muscleGroups,
            advanceTrainingTechniques
        );
        TemplateSet set2 = TemplateSet.Create
        (
            exercise2.Id,
            repetitionRange,
            8,
            muscleGroups,
            advanceTrainingTechniques
        );
        TemplateSet set3 = TemplateSet.Create
        (
            exercise3.Id,
            repetitionRange,
            8,
            muscleGroups,
            advanceTrainingTechniques
        );
        templateSession.AddSet(set1);
        templateSession.AddSet(set2);
        templateSession.AddSet(set3);
        trainingTemplate.AddSessionTemplate(templateSession);
        _programRepository
            .GetByTrainingTemplateIdAsync(trainingTemplate.Id)
                .Returns(program);
        GetNumberOfSeriesPerGroupPerSessionQuery query =
            new GetNumberOfSeriesPerGroupPerSessionQuery
            (
               trainingTemplate.Id
            );
        GetNumberOfSeriesPerGroupPerSessionQueryHandler queryHandler =
            new GetNumberOfSeriesPerGroupPerSessionQueryHandler(_programRepository);
        // Act
        var result = await queryHandler.HandleAsync(query,default);
        // Assert
        Assert.Single(result);
        Assert.Equal(templateSession.Id, result[0].SessionId);
        Assert.All(result, r => Assert.Equal(6, r.Series["Biceps"]));
        Assert.All(result, r => Assert.Equal(3, r.Series["Back"]));
        Assert.All(result, r => Assert.Equal(3, r.Series["Chest"]));
    }

    // ---------------------------------------------------------------- GetTemplateSessionsFromTrainingTemplate

    [Fact]
    public async Task GetTemplateSessionsQuery_ReturnsTheActiveSessionsInCreationOrderWithTheirCounts()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        (Program program, TrainingTemplate template) = CreateProgramWithTemplate(userId);
        TemplateSession push = TemplateSession.Create("Push");
        TemplateSession pull = TemplateSession.Create("Pull");
        TemplateSession removed = TemplateSession.Create("Removed");
        template.AddSessionTemplate(push);
        template.AddSessionTemplate(pull);
        template.AddSessionTemplate(removed);
        var bench = Guid.CreateVersion7();
        var press = Guid.CreateVersion7();
        push.AddSet(TemplateSet.Create(bench, RepetitionRange.Create(8, 6), 8, new List<MuscleGroups> { MuscleGroups.Chest }));
        push.AddSet(TemplateSet.Create(bench, RepetitionRange.Create(8, 6), 8, new List<MuscleGroups> { MuscleGroups.Chest }));
        push.AddSet(TemplateSet.Create(press, RepetitionRange.Create(10, 8), 8, new List<MuscleGroups> { MuscleGroups.Chest }));
        template.DeleteSessionTemplate(removed.Id, DateTime.UtcNow);
        _programRepository.GetByTrainingTemplateIdAsync(template.Id).Returns(program);
        var queryHandler = new GetTemplateSessionsFromTrainingTemplateQueryHandler(_programRepository);
        // Act
        var result = await queryHandler.HandleAsync(new GetTemplateSessionsFromTrainingTemplateQuery(template.Id, userId), default);
        // Assert
        Assert.Equal(new[] { "Push", "Pull" }, result.Select(s => s.Name));
        Assert.Equal(push.Id, result[0].Id);
        Assert.Equal(3, result[0].SetsCount);
        Assert.Equal(2, result[0].ExercisesCount);
        Assert.Equal(0, result[1].SetsCount);
    }

    [Fact]
    public async Task GetTemplateSessionsQuery_OfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        (Program program, TrainingTemplate template) = CreateProgramWithTemplate(Guid.CreateVersion7());
        _programRepository.GetByTrainingTemplateIdAsync(template.Id).Returns(program);
        var queryHandler = new GetTemplateSessionsFromTrainingTemplateQueryHandler(_programRepository);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => queryHandler.HandleAsync(new GetTemplateSessionsFromTrainingTemplateQuery(template.Id, Guid.CreateVersion7()), default)
        );
    }

    [Fact]
    public async Task GetTemplateSessionsQuery_NotFound_ThrowsApplicationException()
    {
        // Arrange
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        _programRepository.GetByTrainingTemplateIdAsync(Arg.Any<Guid>()).Returns((Program?)null);
        var queryHandler = new GetTemplateSessionsFromTrainingTemplateQueryHandler(_programRepository);
        // Act
        // Assert
        await Assert.ThrowsAsync<TrainingTemplateNotFoundApplicationException>
        (
            () => queryHandler.HandleAsync(new GetTemplateSessionsFromTrainingTemplateQuery(Guid.CreateVersion7(), Guid.CreateVersion7()), default)
        );
    }
}

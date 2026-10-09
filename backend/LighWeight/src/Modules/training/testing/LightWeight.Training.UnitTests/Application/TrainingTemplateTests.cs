using LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;
using LightWeight.Training.Application.Commands.TrainingTemplates.DeleteTrainingTemplate;
using LightWeight.Training.Application.Commands.TrainingTemplates.DuplicateTrainingTemplate;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Application.Queries.TrainingTemplates.GetUserTrainingTemplates;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Domain.ValueObjects;
using NSubstitute;

namespace LightWeight.Training.UnitTests.Application;

public class TrainingTemplateTests
{
    private static Program CreateProgram(Guid userId)
    {
        return Program.Create
        (
            userId,
            Periodization.Linear,
            "ValidProgramName",
            new List<MuscleGroups>()
        );
    }

    [Theory]
    [InlineData("MV","FullBody",7,1)]
    [InlineData("MAV","UpperLower",5,2)]
    [InlineData("MRV","PushPullLegs",10,3)]
    public async Task CreateTrainingTemplateCommand_WithValidData_AddsTheTemplateToTheProgram
    (
        string VolumeLandmark,
        string TrainingDistribution,
        int DurationInDays,
        int Order
    )
    {
        // Arrange
        Guid UserId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _Uow = Substitute.For<ITrainingUnitOfWork>();
        Program program = CreateProgram(UserId);
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateTrainingTemplateCommandHandler commandHandler = new CreateTrainingTemplateCommandHandler
        (
            _programRepository,
            _Uow
        );
        CreateTrainingTemplateCommand command = new CreateTrainingTemplateCommand
        (
            program.Id,
            UserId,
            VolumeLandmark,
            TrainingDistribution,
            DurationInDays,
            Order
        );
        // Act
        Guid TemplateId = await commandHandler.HandleAsync(command,default);
        // Assert
        Assert.NotEqual(Guid.Empty,TemplateId);
        TrainingTemplate template = Assert.Single(program.trainingTemplates);
        Assert.Equal(TemplateId, template.Id);
        Assert.Equal(DurationInDays, template.DurationInDays);
        Assert.Equal(Order, template.Order);
        await _Uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTrainingTemplateCommand_ProgramNotFound_ThrowApplicationException()
    {
        // Arrange
        var fakeProgramId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _Uow = Substitute.For<ITrainingUnitOfWork>();
        _programRepository.GetByIdAsync(fakeProgramId).Returns((Program?) null);
        CreateTrainingTemplateCommandHandler commandHandler =
            new CreateTrainingTemplateCommandHandler(_programRepository,_Uow);
        // Act
        // Assert
        await Assert.ThrowsAsync<ProgramNotFoundApplicationException>
        (
            () => commandHandler.HandleAsync(new CreateTrainingTemplateCommand(fakeProgramId,Guid.CreateVersion7(),"MV","FullBody",7,1),default)
        );
        await _Uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTrainingTemplateCommand_UserIdDoesNotCorrespondWithProgramUserId_ThrowApplicationException()
    {
        // Arrange
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _Uow = Substitute.For<ITrainingUnitOfWork>();
        Program program = CreateProgram(Guid.CreateVersion7());
        _programRepository.GetByIdAsync(program.Id).Returns(program);
        CreateTrainingTemplateCommandHandler commandHandler =
            new CreateTrainingTemplateCommandHandler(_programRepository,_Uow);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => commandHandler.HandleAsync(new CreateTrainingTemplateCommand(program.Id,Guid.CreateVersion7(),"MV","FullBody",7,1),default)
        );
        Assert.Empty(program.trainingTemplates);
        await _Uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTrainingTemplateCommand_WhenHappyPath_SoftDeletesTheTemplateAndSaves()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _Uow = Substitute.For<ITrainingUnitOfWork>();
        Program program = CreateProgram(userId);
        TrainingTemplate template = TrainingTemplate.Create(VolumeLandmarks.MEV,TrainingDistribution.FullBody,7,1);
        TemplateSession session = TemplateSession.Create("ValidName");
        template.AddSessionTemplate(session);
        program.AddTrainingTemplate(template);
        _programRepository.GetByTrainingTemplateIdAsync(template.Id).Returns(program);
        DeleteTrainingTemplateCommandHandler commandHandler =
            new DeleteTrainingTemplateCommandHandler(_Uow,_programRepository);
        // Act
        await commandHandler.HandleAsync(new DeleteTrainingTemplateCommand(template.Id,userId),default);
        // Assert
        Assert.True(template.IsDeleted);
        Assert.True(session.IsDeleted);
        Assert.Empty(program.trainingTemplates);
        await _Uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTrainingTemplateCommand_UserIdDoesNotCorrespondWithProgramUserId_ThrowApplicationException()
    {
        // Arrange
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _Uow = Substitute.For<ITrainingUnitOfWork>();
        Program program = CreateProgram(Guid.CreateVersion7());
        TrainingTemplate template = TrainingTemplate.Create(VolumeLandmarks.MEV,TrainingDistribution.FullBody,7,1);
        program.AddTrainingTemplate(template);
        _programRepository.GetByTrainingTemplateIdAsync(template.Id).Returns(program);
        DeleteTrainingTemplateCommandHandler commandHandler =
            new DeleteTrainingTemplateCommandHandler(_Uow,_programRepository);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => commandHandler.HandleAsync(new DeleteTrainingTemplateCommand(template.Id,Guid.CreateVersion7()),default)
        );
        Assert.False(template.IsDeleted);
        await _Uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateTrainingTemplateCommand_WhenHappyPath_AddsTheCopyToTheProgramAndSaves()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _Uow = Substitute.For<ITrainingUnitOfWork>();
        Program program = CreateProgram(userId);
        TrainingTemplate template = TrainingTemplate.Create(VolumeLandmarks.MEV,TrainingDistribution.FullBody,7,1);
        template.AddSessionTemplate(TemplateSession.Create("ValidName"));
        program.AddTrainingTemplate(template);
        _programRepository.GetByTrainingTemplateIdAsync(template.Id).Returns(program);
        DuplicateTrainingTemplateCommandHandler commandHandler =
            new DuplicateTrainingTemplateCommandHandler(_programRepository,_Uow);
        // Act
        Guid copyId = await commandHandler.HandleAsync(new DuplicateTrainingTemplateCommand(template.Id,userId),default);
        // Assert
        Assert.NotEqual(template.Id, copyId);
        TrainingTemplate copy = Assert.Single(program.trainingTemplates, t => t.Id == copyId);
        Assert.Single(copy.TemplateSessions);
        await _Uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateTrainingTemplateCommand_TemplateNotFound_ThrowApplicationException()
    {
        // Arrange
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _Uow = Substitute.For<ITrainingUnitOfWork>();
        _programRepository.GetByTrainingTemplateIdAsync(Arg.Any<Guid>()).Returns((Program?)null);
        DuplicateTrainingTemplateCommandHandler commandHandler =
            new DuplicateTrainingTemplateCommandHandler(_programRepository,_Uow);
        // Act
        // Assert
        await Assert.ThrowsAsync<TrainingTemplateNotFoundApplicationException>
        (
            () => commandHandler.HandleAsync(new DuplicateTrainingTemplateCommand(Guid.CreateVersion7(),Guid.CreateVersion7()),default)
        );
        await _Uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateTrainingTemplateCommand_UserIdDoesNotCorrespondWithProgramUserId_ThrowApplicationException()
    {
        // Arrange
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        ITrainingUnitOfWork _Uow = Substitute.For<ITrainingUnitOfWork>();
        Program program = CreateProgram(Guid.CreateVersion7());
        TrainingTemplate template = TrainingTemplate.Create(VolumeLandmarks.MEV,TrainingDistribution.FullBody,7,1);
        program.AddTrainingTemplate(template);
        _programRepository.GetByTrainingTemplateIdAsync(template.Id).Returns(program);
        DuplicateTrainingTemplateCommandHandler commandHandler =
            new DuplicateTrainingTemplateCommandHandler(_programRepository,_Uow);
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>
        (
            () => commandHandler.HandleAsync(new DuplicateTrainingTemplateCommand(template.Id,Guid.CreateVersion7()),default)
        );
        Assert.Single(program.trainingTemplates);
        await _Uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUserTrainingTemplate_ReturnsDataSuccessfullyAsync()
    {
        var userId = Guid.CreateVersion7();
        IProgramRepository _programRepository = Substitute.For<IProgramRepository>();
        Program program = CreateProgram(userId);
        TrainingTemplate trainingTemplate = TrainingTemplate.Create
        (
            VolumeLandmarks.MAV,
            TrainingDistribution.FullBody,
            7,
            1
        );
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
        program.AddTrainingTemplate(trainingTemplate);
        _programRepository
            .GetAllProgramsOfAUserAsync(userId)
                .Returns(new List<Program> { program });
        GetUserTrainingTemplatesQuery query = new GetUserTrainingTemplatesQuery
        (
            userId
        );
        GetUserTrainingTemplatesQueryHandler queryHandler =
        new GetUserTrainingTemplatesQueryHandler(_programRepository);
        // act
        var result = await queryHandler.HandleAsync(query,default);
        // assert
        var response = Assert.Single(result);
        Assert.Equal(trainingTemplate.Id, response.Id);
        Assert.Equal(program.Id, response.ProgramId);
        Assert.Equal(program.Name, response.ProgramName);
        Assert.Equal(6, response.TotalVolume["Biceps"]);
        Assert.Equal(3, response.TotalVolume["Back"]);
        Assert.Equal(3, response.TotalVolume["Chest"]);
    }
}

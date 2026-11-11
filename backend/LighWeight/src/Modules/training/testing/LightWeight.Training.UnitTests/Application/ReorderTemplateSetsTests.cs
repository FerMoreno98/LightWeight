using LightWeight.Training.Application.Commands.TemplateSets.ReorderTemplateSets;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Domain.ValueObjects;
using NSubstitute;

namespace LightWeight.Training.UnitTests.Application;

public class ReorderTemplateSetsTests
{
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly IProgramRepository _programs = Substitute.For<IProgramRepository>();
    private readonly ITrainingUnitOfWork _uow = Substitute.For<ITrainingUnitOfWork>();
    private readonly TemplateSession _session = TemplateSession.Create("Torso A");
    private readonly List<TemplateSet> _sets = new();

    public ReorderTemplateSetsTests()
    {
        Program program = Program.Create(_userId, Periodization.Linear, "Programa", new List<MuscleGroups>());
        TrainingTemplate template = TrainingTemplate.Create("Base", VolumeLandmarks.MEV, TrainingDistribution.FullBody, 7);
        for (int i = 0; i < 3; i++)
        {
            TemplateSet set = TemplateSet.Create(Guid.NewGuid(), RepetitionRange.Create(8, 12), 8, new List<MuscleGroups> { MuscleGroups.Chest });
            _session.AddSet(set);
            _sets.Add(set);
        }
        template.AddSessionTemplate(_session);
        program.AddTrainingTemplate(template);
        _programs.GetByTemplateSessionIdAsync(_session.Id).Returns(program);
    }

    private ReorderTemplateSetsCommandHandler Handler() => new(_programs, _uow);

    [Fact]
    public async Task ReorderTemplateSets_OfTheUser_ChangesTheOrderAndSaves()
    {
        // Arrange
        List<Guid> newOrder = new() { _sets[2].Id, _sets[0].Id, _sets[1].Id };
        // Act
        await Handler().HandleAsync(new ReorderTemplateSetsCommand(_session.Id, _userId, newOrder));
        // Assert
        Assert.Equal(newOrder, _session.TemplateExercises.Select(s => s.Id));
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderTemplateSets_SessionOfAnotherUser_ThrowsUnauthorized()
    {
        // Arrange
        List<Guid> before = _session.TemplateExercises.Select(s => s.Id).ToList();
        List<Guid> newOrder = new() { _sets[2].Id, _sets[0].Id, _sets[1].Id };
        // Act
        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => Handler().HandleAsync(new ReorderTemplateSetsCommand(_session.Id, Guid.CreateVersion7(), newOrder)));
        Assert.Equal(before, _session.TemplateExercises.Select(s => s.Id));
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderTemplateSets_SessionNotFound_ThrowsNotFound()
    {
        // Act
        // Assert
        await Assert.ThrowsAsync<TemplateSessionNotFoundApplicationException>(
            () => Handler().HandleAsync(new ReorderTemplateSetsCommand(Guid.CreateVersion7(), _userId, _sets.Select(s => s.Id).ToList())));
    }

    [Fact]
    public async Task ReorderTemplateSets_WithAnIncompleteList_ThrowsAndDoesNotSave()
    {
        // Act
        // Assert
        await Assert.ThrowsAsync<InvalidSetOrderDomainException>(
            () => Handler().HandleAsync(new ReorderTemplateSetsCommand(_session.Id, _userId, new List<Guid> { _sets[0].Id })));
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

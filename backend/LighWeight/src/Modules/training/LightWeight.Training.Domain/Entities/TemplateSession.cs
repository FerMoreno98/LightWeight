using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Training.Domain.Entities;

public sealed class TemplateSession : Entity<Guid>
{
    public string Name{get; private set;}
    private List<TemplateExercise> _templateExercise = new();

    private TemplateSession
    (
        Guid Id,
        string name
    ) : base(Id)
    {
        Name = name;
    }

    public IReadOnlyCollection<TemplateExercise> TemplateExercises => _templateExercise.AsReadOnly();
}
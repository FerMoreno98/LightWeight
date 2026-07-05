using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Training.Domain.Entities;

public sealed class TemplateSession : Entity<Guid>
{
    /// <summary>Name of the template session (e.g. "Push A", "Upper")</summary>
    public string Name{get; private set;}
    private List<TemplateSet> _templateExercise = new();

    private TemplateSession
    (
        Guid Id,
        string name
    ) : base(Id)
    {
        Name = name;
    }

    /// <summary>Planned sets for this session</summary>
    public IReadOnlyCollection<TemplateSet> TemplateExercises => _templateExercise.AsReadOnly();

    /// <summary>Creates a new template session</summary>
    /// <param name="name">Session name</param>
    public static TemplateSession Create
    (
        string name
    )
    {
        return new TemplateSession
        (
            Guid.CreateVersion7(),
            name
        );
    }
}
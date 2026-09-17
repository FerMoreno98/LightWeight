using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class Program : AggregateRoot<Guid>
{
    public Guid UserId{get; private set;}
    /// <summary>Type of periodization used</summary>
    public Periodization Periodization{get;private set;}
    private List<TrainingTemplate> _trainingTemplates = new();
    public IReadOnlyCollection<TrainingTemplate> trainingTemplates => _trainingTemplates.AsReadOnly();
    public List<MuscleGroups> AimMuscleGroups {get; private set;}

    private Program
    (
        Guid Id,
        Guid userId,
        Periodization periodization,
        List<MuscleGroups> muscleGroups

    ) : base(Id)
    {
        UserId = userId;
        Periodization = periodization;
        AimMuscleGroups = muscleGroups;
    }
    public static Program Create
    (
        Guid UserId,
        Periodization periodization,
        List<MuscleGroups> muscleGroups
    )
    {
        return new Program(Guid.CreateVersion7(),UserId,periodization,muscleGroups);
    }
}
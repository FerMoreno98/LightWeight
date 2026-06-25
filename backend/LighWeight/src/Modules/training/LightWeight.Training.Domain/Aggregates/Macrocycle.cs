using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class Macrocycle : AggregateRoot<Guid>
{

    private Macrocycle
    (
        Guid Id,
        Guid userId,
        List<MuscleGroups> aimMuscleGroups, 
        DateTime startAt, 
        TrainingStage stage
    ) : base(Id)
    {
        UserId = userId;
        _aimMuscleGroups = aimMuscleGroups;
        StartAt = startAt;
        Stage = stage;
    }
    public Guid UserId {get; private set;}
   private List<MuscleGroups> _aimMuscleGroups = new();
    public IReadOnlyCollection<MuscleGroups> AimMuscleGroups => _aimMuscleGroups.AsReadOnly();
    public DateTime StartAt{get; private set;}
    public TrainingStage Stage{get;private set;}

    public static Macrocycle Create
    (
        Guid UserId,
        List<MuscleGroups> AimMuscles, 
        DateTime StartedAt, 
        TrainingStage stage
    )
    {
        return new Macrocycle(Guid.CreateVersion7(),UserId,AimMuscles,StartedAt,stage);
    }


}
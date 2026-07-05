using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class Exercise : AggregateRoot<Guid>
{
    private Exercise
    (
        Guid id,
        string name,
        bool isBilateral,
        List<MuscleGroups> aimMuscleGroups
    ) : base(id)
    {
        Name = name;
        IsBilateral = isBilateral;
        _aimMuscleGroups = aimMuscleGroups;
    }

    /// <summary>Name of the exercise</summary>
    public string Name { get; private set; }
    /// <summary>Indicates whether the exercise involves both limbs simultaneously</summary>
    public bool IsBilateral { get; private set; }
    private List<MuscleGroups> _aimMuscleGroups = new();
    /// <summary>Muscle groups that this exercise can target</summary>
    public IReadOnlyCollection<MuscleGroups> AimMuscleGroups => _aimMuscleGroups.AsReadOnly();

    /// <summary>Creates a new exercise</summary>
    /// <param name="name">Name of the exercise</param>
    /// <param name="isBilateral">Whether it involves both limbs</param>
    /// <param name="aimMuscleGroups">Muscle groups this exercise can work</param>
    public static Exercise Create
    (
        string name,
        bool isBilateral,
        List<MuscleGroups> aimMuscleGroups
    )
    {
        return new Exercise
        (
            Guid.CreateVersion7(),
            name,
            isBilateral,
            aimMuscleGroups
        );
    }
}

using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class Exercise : AggregateRoot<Guid>
{
    private Exercise
    (
        Guid id,
        string name,
        Laterality laterality,
        List<MuscleGroups> primaryMuscleGroups,
        List<MuscleGroups> secondaryMuscleGroups
    ) : base(id)
    {
        Name = name;
        Laterality = laterality;
        _primaryMuscleGroups = primaryMuscleGroups;
        _secondaryMuscleGroups = secondaryMuscleGroups;
    }

    /// <summary>Name of the exercise</summary>
    public string Name { get; private set; }
    /// <summary>Whether it is performed with both limbs, one at a time or either way</summary>
    public Laterality Laterality { get; private set; }
    private List<MuscleGroups> _primaryMuscleGroups = new();
    private List<MuscleGroups> _secondaryMuscleGroups = new();
    /// <summary>Muscle groups this exercise mainly targets</summary>
    public IReadOnlyCollection<MuscleGroups> PrimaryMuscleGroups => _primaryMuscleGroups.AsReadOnly();
    /// <summary>Muscle groups this exercise also works, to a lesser degree</summary>
    public IReadOnlyCollection<MuscleGroups> SecondaryMuscleGroups => _secondaryMuscleGroups.AsReadOnly();
    /// <summary>Muscle groups that this exercise can target: the primary ones followed by the secondary ones</summary>
    public IReadOnlyCollection<MuscleGroups> AimMuscleGroups => _primaryMuscleGroups.Concat(_secondaryMuscleGroups).ToList().AsReadOnly();

    /// <summary>Creates a new exercise</summary>
    /// <param name="name">Name of the exercise</param>
    /// <param name="laterality">Whether it is performed with both limbs, one at a time or either way</param>
    /// <param name="primaryMuscleGroups">Muscle groups this exercise mainly works</param>
    /// <param name="secondaryMuscleGroups">Muscle groups this exercise also works (none if omitted)</param>
    public static Exercise Create
    (
        string name,
        Laterality laterality,
        List<MuscleGroups> primaryMuscleGroups,
        List<MuscleGroups>? secondaryMuscleGroups = null
    )
    {
        return new Exercise
        (
            Guid.CreateVersion7(),
            name,
            laterality,
            primaryMuscleGroups,
            secondaryMuscleGroups ?? new()
        );
    }

    /// <summary>Replaces the laterality and the muscle groups, keeping the exercise (and the sets that use it)</summary>
    public void UpdateClassification
    (
        Laterality laterality,
        List<MuscleGroups> primaryMuscleGroups,
        List<MuscleGroups> secondaryMuscleGroups
    )
    {
        Laterality = laterality;
        _primaryMuscleGroups = primaryMuscleGroups;
        _secondaryMuscleGroups = secondaryMuscleGroups;
    }
}

using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Domain.Entities;

public sealed class TemplateSet : Entity<Guid>
{
    /// <summary>Exercise planned for this set</summary>
    public Guid ExerciseId { get; private set; }
    /// <summary>Planned repetition range (min-max)</summary>
    public RepetitionRange RepetitionRange { get; private set; }
    /// <summary>Expected repetitions in reserve (how many reps left before failure)</summary>
    public decimal ExpectedRPE { get; private set; }
    /// <summary>Advanced technique planned, if any</summary>
    public AdvanceTrainingTechniques? AdvanceTrainingTechniques { get; private set; }
    /// <summary>Shared ID with other template sets that form a planned superset</summary>
    public Guid? SuperSetGroupId { get; private set; }
    /// <summary>
    /// Represents the muscle groups that the set is going to emphazise 
    /// </summary>
    public List<MuscleGroups> AimMuscleGroups {get; private set;}
    /// <summary>Soft delete flag: deleted sets are kept so performed sets can still reference them</summary>
    public bool IsDeleted { get; private set; }
    /// <summary>Date the set was soft deleted (null while active)</summary>
    public DateTime? DeletedAt { get; private set; }

    private TemplateSet
    (
        Guid Id,
        Guid exerciseId,
        decimal expectedRPE,
        List<MuscleGroups> aimMuscleGroups,
        Guid? superSetGroupId
    ) : base(Id)
    {
        ExerciseId = exerciseId;
        RepetitionRange = null!;
        ExpectedRPE = expectedRPE;
        AdvanceTrainingTechniques = null!;
        AimMuscleGroups = aimMuscleGroups;
        SuperSetGroupId = superSetGroupId;
    }

    /// <summary>Creates a planned set for a template</summary>
    /// <param name="exerciseId">The exercise to perform</param>
    /// <param name="repetitionRange">Target rep range</param>
    /// <param name="expectedRIR">Reps in reserve target</param>
    /// <param name="advanceTrainingTechniques">Advanced technique to apply</param>
    /// <param name="superSetGroupId">Superset group ID if applicable</param>
    public static TemplateSet Create
    (
        Guid exerciseId,
        RepetitionRange repetitionRange,
        decimal expectedRPE,
        List<MuscleGroups> aimMuscleGroups,
        AdvanceTrainingTechniques? advanceTrainingTechniques = null,
        Guid? superSetGroupId = null
    )
    {
        return new TemplateSet
        (
            Guid.CreateVersion7(),
            exerciseId,
            expectedRPE,
            aimMuscleGroups,
            superSetGroupId
        )
        {
            RepetitionRange = repetitionRange,
            AdvanceTrainingTechniques = advanceTrainingTechniques ?? AdvanceTrainingTechniques.None
        };
    }
    public void UpdateSet
    (
        RepetitionRange repetitionRange,
        decimal expectedRPE,
        List<MuscleGroups> aimMuscleGroups,
        AdvanceTrainingTechniques? advanceTrainingTechniques = null,
        Guid? superSetGroupId = null
    )
    {
        RepetitionRange = repetitionRange;
        ExpectedRPE = expectedRPE;
        AimMuscleGroups = aimMuscleGroups;
        AdvanceTrainingTechniques = advanceTrainingTechniques;
        SuperSetGroupId = superSetGroupId;

    }

    /// <summary>Marks the set as deleted without removing it from the database</summary>
    /// <param name="now">Deletion timestamp</param>
    internal void MarkAsDeleted(DateTime now)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = now;
    }
}
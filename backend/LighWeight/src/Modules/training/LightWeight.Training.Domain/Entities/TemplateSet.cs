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
    public int ExpectedRIR { get; private set; }
    /// <summary>Advanced technique planned, if any</summary>
    public AdvanceTrainingTechniques AdvanceTrainingTechniques { get; private set; }
    /// <summary>Shared ID with other template sets that form a planned superset</summary>
    public Guid? SuperSetGroupId { get; private set; }

    private TemplateSet
    (
        Guid Id,
        Guid exerciseId,
        RepetitionRange repetitionRange,
        int expectedRIR,
        AdvanceTrainingTechniques advanceTrainingTechniques,
        Guid? superSetGroupId
    ) : base(Id)
    {
        ExerciseId = exerciseId;
        RepetitionRange = repetitionRange;
        ExpectedRIR = expectedRIR;
        AdvanceTrainingTechniques = advanceTrainingTechniques;
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
        int expectedRIR,
        AdvanceTrainingTechniques? advanceTrainingTechniques = null,
        Guid? superSetGroupId = null
    )
    {
        return new TemplateSet
        (
            Guid.CreateVersion7(),
            exerciseId,
            repetitionRange,
            expectedRIR,
            advanceTrainingTechniques ?? AdvanceTrainingTechniques.None,
            superSetGroupId
        );
    }
}
using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Domain.Entities;

public sealed class Set : Entity<Guid>
{
    private Set
    (
        Guid Id,
        Guid exerciseId,
        int repetitions,
        bool isBodyWeight,
        AdvanceTrainingTechniques advanceTrainingTechniques,
        decimal weight,
        decimal rPE,
        Guid? superSetGroupId
    ) : base(Id)
    {
        ExerciseId = exerciseId;
        Repetitions = repetitions;
        IsBodyWeight = isBodyWeight;
        AdvanceTrainingTechniques = advanceTrainingTechniques;
        Weight = weight;
        RPE = rPE;
        SuperSetGroupId = superSetGroupId;
    }

    /// <summary>Exercise performed in this set</summary>
    public Guid ExerciseId { get; private set; }
    /// <summary>Number of repetitions performed</summary>
    public int Repetitions { get; private set; }
    /// <summary>Whether the set uses only body weight (no external load)</summary>
    public bool IsBodyWeight { get; private set; }
    /// <summary>Advanced technique applied (drop set, cluster, myo-rep), if any</summary>
    public AdvanceTrainingTechniques AdvanceTrainingTechniques { get; private set; }
    /// <summary>Shared ID with other sets that form a superset (null if not a superset)</summary>
    public Guid? SuperSetGroupId { get; private set; }
    /// <summary>Load used in kg</summary>
    public decimal Weight { get; private set; }
    /// <summary>Rate of perceived exertion (1-10)</summary>
    public decimal RPE { get; private set; }

    /// <summary>Creates a new performed set</summary>
    /// <param name="exerciseId">Exercise performed</param>
    /// <param name="repetitions">Reps completed</param>
    /// <param name="isBodyWeight">Whether it was bodyweight only</param>
    /// <param name="advanceTrainingTechniques">Advanced technique applied</param>
    /// <param name="weight">Load in kg</param>
    /// <param name="rPE">RPE (1-10)</param>
    /// <param name="superSetGroupId">Superset group ID if applicable</param>
    public static Set Create
    (
        Guid exerciseId,
        int repetitions,
        bool isBodyWeight,
        AdvanceTrainingTechniques advanceTrainingTechniques,
        decimal weight,
        decimal rPE,
        Guid? superSetGroupId = null
    )
    {
        return new Set
        (
            Guid.CreateVersion7(),
            exerciseId,
            repetitions,
            isBodyWeight,
            advanceTrainingTechniques,
            weight,
            rPE,
            superSetGroupId
        );
    }
}
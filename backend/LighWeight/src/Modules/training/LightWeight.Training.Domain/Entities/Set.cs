using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Domain.Entities;

/// <summary>
/// A set actually performed in a TrainingSession. Only the session creates and changes them (see TrainingSession.RegisterSet)
/// </summary>
public sealed class Set : Entity<Guid>
{
    /// <summary>Lowest RPE that can be recorded</summary>
    public const decimal MinRPE = 1;
    /// <summary>Highest RPE that can be recorded</summary>
    public const decimal MaxRPE = 10;

    // EF Core cannot bind complex properties to constructor parameters, so AdvanceTrainingTechniques is set in Create
    private Set
    (
        Guid Id,
        Guid exerciseId,
        int repetitions,
        bool isBodyWeight,
        decimal weight,
        decimal rPE,
        Guid? superSetGroupId,
        Guid? templateSetId
    ) : base(Id)
    {
        ExerciseId = exerciseId;
        Repetitions = repetitions;
        IsBodyWeight = isBodyWeight;
        AdvanceTrainingTechniques = null!;
        Weight = weight;
        RPE = rPE;
        SuperSetGroupId = superSetGroupId;
        TemplateSetId = templateSetId;
    }

    /// <summary>Exercise actually performed (it may differ from the planned one when it was replaced)</summary>
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
    /// <summary>Planned set this set covers (null for an extra set)</summary>
    public Guid? TemplateSetId{get;private set;}

    /// <summary>Creates a new performed set</summary>
    /// <param name="exerciseId">Exercise performed</param>
    /// <param name="repetitions">Reps completed</param>
    /// <param name="isBodyWeight">Whether it was bodyweight only</param>
    /// <param name="advanceTrainingTechniques">Advanced technique applied</param>
    /// <param name="weight">Load in kg</param>
    /// <param name="rPE">RPE (1-10)</param>
    /// <param name="superSetGroupId">Superset group ID if applicable</param>
    /// <param name="TemplateSetId">Planned set it covers, if any</param>
    internal static Set Create
    (
        Guid exerciseId,
        int repetitions,
        bool isBodyWeight,
        AdvanceTrainingTechniques advanceTrainingTechniques,
        decimal weight,
        decimal rPE,
        Guid? superSetGroupId = null,
        Guid? TemplateSetId = null
    )
    {
        EnsureValidValues(repetitions, weight, rPE);
        return new Set
        (
            Guid.CreateVersion7(),
            exerciseId,
            repetitions,
            isBodyWeight,
            weight,
            rPE,
            superSetGroupId,
            TemplateSetId
        )
        {
            AdvanceTrainingTechniques = advanceTrainingTechniques
        };
    }

    /// <summary>Corrects what was performed in this set</summary>
    internal void Update(Guid exerciseId, int repetitions, decimal weight, decimal rPE, bool isBodyWeight)
    {
        EnsureValidValues(repetitions, weight, rPE);
        ExerciseId = exerciseId;
        Repetitions = repetitions;
        Weight = weight;
        RPE = rPE;
        IsBodyWeight = isBodyWeight;
    }

    /// <summary>Joins the set to a superset, or takes it out with null</summary>
    internal void SetSuperSet(Guid? superSetGroupId)
    {
        SuperSetGroupId = superSetGroupId;
    }

    private static void EnsureValidValues(int repetitions, decimal weight, decimal rPE)
    {
        if(repetitions < 0 || weight < 0 || rPE < MinRPE || rPE > MaxRPE)
            throw new InvalidSetValuesDomainException();
    }
}

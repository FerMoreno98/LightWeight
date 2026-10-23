namespace LightWeight.Dashboard.Application.Projections.Training;

// Read models fed by the "session finished" event of training. A finished session never changes, so they are
// written once (and again, identical, when the dashboard rebuilds). Keys are the ids of the source

/// <summary>A finished training session of a user, with the wellbeing recorded when it started</summary>
public sealed class TrainingSessionProjection
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    // The cycles it belongs to: the future "macrocycle / mesocycle" filter is a plain WHERE on them
    public Guid MacrocycleId { get; set; }
    public Guid MesocycleId { get; set; }
    public Guid MicrocycleId { get; set; }
    public int WeekNumber { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public int Motivation { get; set; }
    public int Sleep { get; set; }
    public int Doms { get; set; }
}

/// <summary>A set performed in a session: what was done and what had been planned for it</summary>
public sealed class TrainingSetProjection
{
    public Guid SetId { get; set; }
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Position in the session (1, 2, 3...)</summary>
    public int Order { get; set; }
    public Guid ExerciseId { get; set; }
    public string ExerciseName { get; set; } = "";
    public int Repetitions { get; set; }
    public decimal Weight { get; set; }
    public decimal Rpe { get; set; }
    public bool IsBodyWeight { get; set; }
    /// <summary>"DropSet", "Cluster", "MyoRep" or null</summary>
    public string? Technique { get; set; }
    public bool IsPlanned { get; set; }
    public List<string> ExerciseMuscleGroups { get; set; } = [];
    /// <summary>Null for an extra set</summary>
    public List<string>? PlannedMuscleGroups { get; set; }
    public int? PlannedMinReps { get; set; }
    public int? PlannedMaxReps { get; set; }
}

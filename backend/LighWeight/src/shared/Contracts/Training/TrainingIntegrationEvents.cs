namespace LightWeight.shared.Contracts.Training;

// Training publishes facts, not interpretations: what was performed and what had been planned for each set.
// How to count volume, estimate strength or group by effort is decided by the consumers (the dashboard).
// Only primitive types, so consumers don't depend on the training domain (muscle groups go as their enum names)

/// <summary>
/// A training session has been finished. A finished session is read only, so this event carries its whole
/// content and no later "corrected" or "deleted" events exist (a session can only be discarded while in progress)
/// </summary>
public sealed record TrainingSessionFinishedIntegrationEvent(
    Guid     SessionId,
    Guid     UserId,
    Guid     MacrocycleId,
    Guid     MesocycleId,
    Guid     MicrocycleId,
    // Number of the week inside its mesocycle (1, 2, 3...)
    int      WeekNumber,
    string   Name,
    DateTime StartedAt,
    DateTime FinishedAt,
    // Recorded when the session started (1-10)
    int      Motivation,
    int      Sleep,
    int      Doms,
    IReadOnlyList<PerformedSetSnapshot> Sets,
    DateTime OccurredAtUtc) : IIntegrationEvent
{
    public string EventName => EventNames.Training.SessionFinished;
}

/// <summary>A set performed in the session, with the data of the planned set it covered (null for an extra set)</summary>
public sealed record PerformedSetSnapshot(
    Guid     SetId,
    // Position of the set in the session (1, 2, 3...), in the order it was registered
    int      Order,
    Guid     ExerciseId,
    string   ExerciseName,
    int      Repetitions,
    // Load in kg (not meaningful when IsBodyWeight)
    decimal  Weight,
    // Rate of perceived exertion (1-10); RIR = 10 - RPE
    decimal  Rpe,
    bool     IsBodyWeight,
    // "DropSet", "Cluster", "MyoRep" or null
    string?  Technique,
    bool     IsPlanned,
    // Muscle groups the performed exercise works
    IReadOnlyList<string> ExerciseMuscleGroups,
    // Muscle groups the planned set aimed at (null for an extra set)
    IReadOnlyList<string>? PlannedMuscleGroups,
    // Repetition range of the planned set (null for an extra set)
    int?     PlannedMinReps,
    int?     PlannedMaxReps);

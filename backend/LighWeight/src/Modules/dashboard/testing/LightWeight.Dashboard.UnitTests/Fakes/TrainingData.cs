using LightWeight.Dashboard.Application.Projections.Training;

namespace LightWeight.Dashboard.UnitTests.Fakes;

/// <summary>Builds training projections for the tests: a session and its sets with short calls</summary>
internal sealed class TrainingData
{
    public static readonly DateTime Monday = new(2026, 10, 12, 18, 0, 0, DateTimeKind.Utc);

    public Guid UserId { get; } = Guid.CreateVersion7();
    public Guid Bench { get; } = Guid.CreateVersion7();
    public Guid Squat { get; } = Guid.CreateVersion7();

    public List<TrainingSessionProjection> Sessions { get; } = [];
    public List<TrainingSetProjection> Sets { get; } = [];

    public TrainingSessionProjection Session(DateTime startedAt, Guid? microcycleId = null, int week = 1, string name = "Torso A", int motivation = 7, int sleep = 7, int doms = 3)
    {
        var session = new TrainingSessionProjection
        {
            SessionId = Guid.CreateVersion7(),
            UserId = UserId,
            MacrocycleId = Guid.Empty,
            MesocycleId = Guid.Empty,
            MicrocycleId = microcycleId ?? Guid.CreateVersion7(),
            WeekNumber = week,
            Name = name,
            StartedAt = startedAt,
            FinishedAt = startedAt.AddHours(1),
            Motivation = motivation,
            Sleep = sleep,
            Doms = doms
        };
        Sessions.Add(session);
        return session;
    }

    /// <summary>A set of the session; planned 8-10 reps aimed at the chest unless told otherwise</summary>
    public TrainingSetProjection Set
    (
        TrainingSessionProjection session,
        Guid exerciseId,
        decimal weight,
        int reps,
        decimal rpe,
        (int Min, int Max)? planned = null,
        bool extra = false,
        string? technique = null,
        bool bodyWeight = false,
        string[]? exerciseMuscles = null,
        string[]? plannedMuscles = null
    )
    {
        var range = extra ? ((int, int)?)null : planned ?? (8, 10);
        var set = new TrainingSetProjection
        {
            SetId = Guid.CreateVersion7(),
            SessionId = session.SessionId,
            UserId = session.UserId,
            Order = Sets.Count(s => s.SessionId == session.SessionId) + 1,
            ExerciseId = exerciseId,
            ExerciseName = exerciseId == Bench ? "Press banca" : "Sentadilla",
            Repetitions = reps,
            Weight = weight,
            Rpe = rpe,
            IsBodyWeight = bodyWeight,
            Technique = technique,
            IsPlanned = !extra,
            ExerciseMuscleGroups = (exerciseMuscles ?? ["Chest", "Triceps"]).ToList(),
            PlannedMuscleGroups = extra ? null : (plannedMuscles ?? ["Chest"]).ToList(),
            PlannedMinReps = range?.Item1,
            PlannedMaxReps = range?.Item2
        };
        Sets.Add(set);
        return set;
    }

    public InMemoryTrainingProjectionStore Store()
    {
        var store = new InMemoryTrainingProjectionStore();
        foreach (var session in Sessions) store.Sessions[session.SessionId] = session;
        foreach (var set in Sets) store.Sets[set.SetId] = set;
        return store;
    }
}

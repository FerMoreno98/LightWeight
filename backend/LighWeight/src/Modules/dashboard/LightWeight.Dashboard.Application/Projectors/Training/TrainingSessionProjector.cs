using LightWeight.Dashboard.Application.Projections.Training;
using LightWeight.shared.Contracts.Training;
using LightWeight.shared.Messaging;

namespace LightWeight.Dashboard.Application.Projectors.Training;

/// <summary>
/// Keeps the training projections in sync. "Session finished" arrives when a session is finished and again,
/// identical, during a rebuild: it always overwrites the session and its sets
/// </summary>
public sealed class TrainingSessionProjector : IIntegrationEventHandler<TrainingSessionFinishedIntegrationEvent>
{
    private readonly ITrainingProjectionStore _store;

    public TrainingSessionProjector(ITrainingProjectionStore store)
        => _store = store;

    public Task HandleAsync(TrainingSessionFinishedIntegrationEvent @event, CancellationToken ct = default)
    {
        var session = new TrainingSessionProjection
        {
            SessionId = @event.SessionId,
            UserId = @event.UserId,
            MacrocycleId = @event.MacrocycleId,
            MesocycleId = @event.MesocycleId,
            MicrocycleId = @event.MicrocycleId,
            WeekNumber = @event.WeekNumber,
            Name = @event.Name,
            StartedAt = @event.StartedAt,
            FinishedAt = @event.FinishedAt,
            Motivation = @event.Motivation,
            Sleep = @event.Sleep,
            Doms = @event.Doms
        };

        var sets = @event.Sets
            .Select(s => new TrainingSetProjection
            {
                SetId = s.SetId,
                SessionId = @event.SessionId,
                UserId = @event.UserId,
                Order = s.Order,
                ExerciseId = s.ExerciseId,
                ExerciseName = s.ExerciseName,
                Repetitions = s.Repetitions,
                Weight = s.Weight,
                Rpe = s.Rpe,
                IsBodyWeight = s.IsBodyWeight,
                Technique = s.Technique,
                IsPlanned = s.IsPlanned,
                ExerciseMuscleGroups = s.ExerciseMuscleGroups.ToList(),
                PlannedMuscleGroups = s.PlannedMuscleGroups?.ToList(),
                PlannedMinReps = s.PlannedMinReps,
                PlannedMaxReps = s.PlannedMaxReps
            })
            .ToList();

        return _store.UpsertSessionAsync(session, sets, ct);
    }
}

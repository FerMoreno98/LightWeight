using LightWeight.shared.Contracts.Training;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Application.Events;

/// <summary>
/// Builds the "session finished" integration event from the session and the data it refers to. Pure: everything it
/// needs is passed in, so the domain event handler and the rebuild handler build exactly the same event
/// </summary>
internal static class TrainingIntegrationEventMapper
{
    /// <param name="session">A finished session with its sets</param>
    /// <param name="macrocycle">The macrocycle that contains the week of the session</param>
    /// <param name="exercises">The exercises performed in the session, by id</param>
    /// <param name="plannedSets">The planned sets the session covered (deleted ones included), by id</param>
    public static TrainingSessionFinishedIntegrationEvent SessionFinished
    (
        TrainingSession session,
        Macrocycle macrocycle,
        IReadOnlyDictionary<Guid, Exercise> exercises,
        IReadOnlyDictionary<Guid, PlannedSetData> plannedSets,
        DateTime occurredAtUtc
    )
    {
        Mesocycle mesocycle = macrocycle.Mesocycles.Single(m => m.Microcycles.Any(w => w.Id == session.MicrocycleId));
        Microcycle week = mesocycle.Microcycles.Single(w => w.Id == session.MicrocycleId);

        // The sets come in the order they were registered (the repository loads them by their Guid v7 id);
        // the event makes that order explicit so consumers don't depend on how ids are generated
        List<PerformedSetSnapshot> sets = session.Sets
            .Select((set, index) => SetSnapshot(set, index + 1, exercises, plannedSets))
            .ToList();

        return new TrainingSessionFinishedIntegrationEvent
        (
            session.Id,
            session.UserId,
            macrocycle.Id,
            mesocycle.Id,
            week.Id,
            week.WeekNumber,
            session.Name,
            session.StartAt,
            session.FinishedAt ?? throw new InvalidOperationException("Only a finished session can be published"),
            session.MotivationLevel,
            session.SleepLevel,
            session.DOMSLevel,
            sets,
            occurredAtUtc
        );
    }

    private static PerformedSetSnapshot SetSnapshot
    (
        Set set,
        int order,
        IReadOnlyDictionary<Guid, Exercise> exercises,
        IReadOnlyDictionary<Guid, PlannedSetData> plannedSets
    )
    {
        exercises.TryGetValue(set.ExerciseId, out Exercise? exercise);
        PlannedSetData? planned = set.TemplateSetId is Guid templateSetId && plannedSets.TryGetValue(templateSetId, out var p) ? p : null;

        return new PerformedSetSnapshot
        (
            set.Id,
            order,
            set.ExerciseId,
            exercise?.Name ?? "?",
            set.Repetitions,
            set.Weight,
            set.RPE,
            set.IsBodyWeight,
            Technique(set.AdvanceTrainingTechniques),
            set.TemplateSetId is not null,
            exercise?.AimMuscleGroups.Select(m => m.ToString()).ToList() ?? [],
            planned?.AimMuscleGroups.Select(m => m.ToString()).ToList(),
            planned?.MinReps,
            planned?.MaxReps
        );
    }

    private static string? Technique(AdvanceTrainingTechniques? techniques)
        => techniques switch
        {
            { IsDropSet: true } => "DropSet",
            { IsCluster: true } => "Cluster",
            { IsMyoRep: true } => "MyoRep",
            _ => null
        };
}

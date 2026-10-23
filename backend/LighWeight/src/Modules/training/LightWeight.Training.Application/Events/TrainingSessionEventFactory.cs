using LightWeight.shared.Contracts.Training;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Events;

/// <summary>
/// Loads what the "session finished" events refer to (exercises and planned sets, in one query each) and builds them.
/// Used for a single session when it is finished and for every session of a user when the dashboard rebuilds
/// </summary>
public sealed class TrainingSessionEventFactory
{
    private readonly IExerciseRepository _exercises;
    private readonly IProgramRepository _programs;

    public TrainingSessionEventFactory(IExerciseRepository exercises, IProgramRepository programs)
    {
        _exercises = exercises;
        _programs = programs;
    }

    /// <param name="sessions">Finished sessions with their sets</param>
    /// <param name="macrocycles">The macrocycles that contain the weeks of those sessions</param>
    public async Task<List<TrainingSessionFinishedIntegrationEvent>> CreateAsync
    (
        IReadOnlyCollection<TrainingSession> sessions,
        IReadOnlyCollection<Macrocycle> macrocycles,
        DateTime occurredAtUtc
    )
    {
        if(sessions.Count == 0) return new();

        var exerciseIds = sessions.SelectMany(s => s.Sets).Select(s => s.ExerciseId).Distinct().ToList();
        var templateSetIds = sessions.SelectMany(s => s.Sets)
            .Where(s => s.TemplateSetId is not null)
            .Select(s => s.TemplateSetId!.Value)
            .Distinct()
            .ToList();

        var exercises = (await _exercises.GetByIdsAsync(exerciseIds)).ToDictionary(e => e.Id);
        var plannedSets = (await _programs.GetPlannedSetsIncludingDeletedAsync(templateSetIds)).ToDictionary(p => p.TemplateSetId);

        var events = new List<TrainingSessionFinishedIntegrationEvent>();
        foreach (TrainingSession session in sessions)
        {
            // A session always belongs to a week of one of the user's macrocycles; if it is not there, it is skipped
            Macrocycle? macrocycle = macrocycles.FirstOrDefault(m =>
                m.Mesocycles.Any(me => me.Microcycles.Any(w => w.Id == session.MicrocycleId)));
            if (macrocycle is null) continue;
            events.Add(TrainingIntegrationEventMapper.SessionFinished(session, macrocycle, exercises, plannedSets, occurredAtUtc));
        }
        return events;
    }
}

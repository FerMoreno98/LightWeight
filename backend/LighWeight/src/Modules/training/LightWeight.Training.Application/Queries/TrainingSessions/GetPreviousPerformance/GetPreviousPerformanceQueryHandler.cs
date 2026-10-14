using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.TrainingSessions.GetPreviousPerformance;

public sealed class GetPreviousPerformanceQueryHandler : IQueryHandler<GetPreviousPerformanceQuery, List<GetPreviousPerformanceResponse>>
{
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly IMacrocycleRepository _macrocycleRepository;

    public GetPreviousPerformanceQueryHandler(ITrainingSessionRepository trainingSessionRepository, IMacrocycleRepository macrocycleRepository)
    {
        _trainingSessionRepository = trainingSessionRepository;
        _macrocycleRepository = macrocycleRepository;
    }

    public async Task<List<GetPreviousPerformanceResponse>> HandleAsync(GetPreviousPerformanceQuery query, CancellationToken ct = default)
    {
        // Only the weeks of the user's active macrocycle are searched, so the data is always the user's own
        Macrocycle? macrocycle = await _macrocycleRepository.GetActiveOfAUserAsync(query.UserId);
        if(macrocycle is null || query.ExerciseIds.Count == 0)
        {
            return new();
        }
        List<Guid> microcycleIds = macrocycle.Mesocycles
            .SelectMany(m => m.Microcycles)
            .Select(m => m.Id)
            .ToList();

        List<ExercisePerformance> performances = await _trainingSessionRepository.GetLastSetsOfExercisesAsync
        (
            microcycleIds,
            query.ExerciseIds,
            query.ExcludeSessionId
        );
        return performances
            .Select(p => new GetPreviousPerformanceResponse
            (
                p.ExerciseId,
                p.PerformedAt,
                p.Sets.Select(s => new PreviousSetResponse(s.Repetitions, s.Weight, s.RPE, s.IsBodyWeight)).ToList()
            ))
            .ToList();
    }
}

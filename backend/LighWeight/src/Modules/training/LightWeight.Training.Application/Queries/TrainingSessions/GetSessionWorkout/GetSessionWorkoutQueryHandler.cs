using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.TrainingSessions.GetSessionWorkout;

public sealed class GetSessionWorkoutQueryHandler : IQueryHandler<GetSessionWorkoutQuery, GetSessionWorkoutResponse>
{
    private readonly ITrainingSessionRepository _trainingSessionRepository;
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly IProgramRepository _programRepository;
    private readonly IExerciseRepository _exerciseRepository;

    public GetSessionWorkoutQueryHandler
    (
        ITrainingSessionRepository trainingSessionRepository,
        IMacrocycleRepository macrocycleRepository,
        IProgramRepository programRepository,
        IExerciseRepository exerciseRepository
    )
    {
        _trainingSessionRepository = trainingSessionRepository;
        _macrocycleRepository = macrocycleRepository;
        _programRepository = programRepository;
        _exerciseRepository = exerciseRepository;
    }

    public async Task<GetSessionWorkoutResponse> HandleAsync(GetSessionWorkoutQuery query, CancellationToken ct = default)
    {
        Program program = await _programRepository.GetByTemplateSessionIdAsync(query.TemplateSessionId)
            ?? throw new TemplateSessionNotFoundApplicationException();
        if(program.UserId != query.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        Macrocycle? macrocycle = await _macrocycleRepository.GetActiveOfAUserAsync(query.UserId);
        Microcycle? week = macrocycle?.ActiveMicrocycle;
        if(week is null)
        {
            return new GetSessionWorkoutResponse(false, null);
        }

        bool belongsToWeek = program.trainingTemplates.Any(t =>
            t.Id == week.TrainingTemplateId && t.TemplateSessions.Any(s => s.Id == query.TemplateSessionId));
        TrainingSession? session = await _trainingSessionRepository.GetByMicrocycleAndTemplateSessionAsync(week.Id, query.TemplateSessionId);
        if(session is null)
        {
            return new GetSessionWorkoutResponse(belongsToWeek, null);
        }

        Dictionary<Guid, string> exerciseNames = (await _exerciseRepository.GetByIdsAsync(session.Sets.Select(s => s.ExerciseId)))
            .ToDictionary(e => e.Id, e => e.Name);
        return new GetSessionWorkoutResponse
        (
            false,
            new WorkoutSessionResponse
            (
                session.Id,
                session.StartAt,
                session.FinishedAt,
                session.Duration,
                session.MotivationLevel,
                session.SleepLevel,
                session.DOMSLevel,
                session.Comments,
                session.Sets
                    .Select(s => new WorkoutSetResponse
                    (
                        s.Id,
                        s.ExerciseId,
                        exerciseNames.GetValueOrDefault(s.ExerciseId, string.Empty),
                        s.TemplateSetId,
                        s.Repetitions,
                        s.Weight,
                        s.RPE,
                        s.IsBodyWeight,
                        s.SuperSetGroupId
                    ))
                    .ToList()
            )
        );
    }
}

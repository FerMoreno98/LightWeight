using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.Exercises.GetAllExercises;

public sealed class GetAllExercisesQueryHandler : IQueryHandler<GetAllExercisesQuery, List<GetAllExercisesResponse>>
{
    private readonly IExerciseRepository _exerciseRepository;

    public GetAllExercisesQueryHandler(IExerciseRepository exerciseRepository)
    {
        _exerciseRepository = exerciseRepository;
    }

    public async Task<List<GetAllExercisesResponse>> HandleAsync(GetAllExercisesQuery query, CancellationToken ct = default)
    {
        List<Exercise> exercises = await _exerciseRepository.GetAllAsync();
        List<GetAllExercisesResponse> ret = new List<GetAllExercisesResponse>();   
        foreach(var exercise in exercises)
        {
            var ex = new GetAllExercisesResponse(exercise.Id,exercise.Name,exercise.IsBilateral,exercise.AimMuscleGroups);
            ret.Add(ex);
        }

        return ret;

    }
}

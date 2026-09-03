using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Application.Queries.Exercises.GetAllExercises;

public sealed record GetAllExercisesQuery : IQuery<List<GetAllExercisesResponse>>;

public sealed record GetAllExercisesResponse
(
    Guid Id,
    string Name,
    bool IsBilateral,
    IReadOnlyCollection<MuscleGroups> AimMuscleGroups
);
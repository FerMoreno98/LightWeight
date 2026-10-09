using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.Programs.GetUserPrograms;

public sealed record GetUserProgramsQuery(Guid UserId) : IQuery<List<GetUserProgramsResponse>>;

public sealed record GetUserProgramsResponse
(
    Guid Id,
    string Name,
    string Periodization,
    IReadOnlyCollection<string> AimMuscleGroups,
    int TrainingTemplatesCount
);

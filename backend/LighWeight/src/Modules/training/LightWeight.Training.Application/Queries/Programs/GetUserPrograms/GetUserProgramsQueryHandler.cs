using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Utils;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.Programs.GetUserPrograms;

public sealed class GetUserProgramsQueryHandler : IQueryHandler<GetUserProgramsQuery, List<GetUserProgramsResponse>>
{
    private readonly IProgramRepository _programRepository;

    public GetUserProgramsQueryHandler(IProgramRepository programRepository)
    {
        _programRepository = programRepository;
    }

    public async Task<List<GetUserProgramsResponse>> HandleAsync(GetUserProgramsQuery query, CancellationToken ct = default)
    {
        List<Program> programs = await _programRepository.GetAllProgramsOfAUserAsync(query.UserId);
        return programs
            .Select(p => new GetUserProgramsResponse
            (
                p.Id,
                p.Name,
                p.Periodization.ToString(),
                p.AimMuscleGroups.Select(Converters.MapMuscleGroup).ToList(),
                p.trainingTemplates.Count
            ))
            .ToList();
    }
}

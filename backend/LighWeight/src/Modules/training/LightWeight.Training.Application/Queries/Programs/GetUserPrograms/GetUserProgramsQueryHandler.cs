using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Projections.UserNames;
using LightWeight.Training.Application.Utils;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.Programs.GetUserPrograms;

public sealed class GetUserProgramsQueryHandler : IQueryHandler<GetUserProgramsQuery, List<GetUserProgramsResponse>>
{
    private readonly IProgramRepository _programRepository;
    private readonly IUserNameProjectionStore _userNames;

    public GetUserProgramsQueryHandler(IProgramRepository programRepository, IUserNameProjectionStore userNames)
    {
        _programRepository = programRepository;
        _userNames = userNames;
    }

    public async Task<List<GetUserProgramsResponse>> HandleAsync(GetUserProgramsQuery query, CancellationToken ct = default)
    {
        List<Program> programs = await _programRepository.GetAllProgramsOfAUserAsync(query.UserId);
        // One query for the current names of the authors of the copied programs
        IReadOnlyDictionary<Guid, string> authorNames = await _userNames.GetNamesAsync(
            programs.Where(p => p.CopiedFromUserId.HasValue).Select(p => p.CopiedFromUserId!.Value), ct);
        return programs
            .Select(p => new GetUserProgramsResponse
            (
                p.Id,
                p.Name,
                p.Periodization.ToString(),
                p.AimMuscleGroups.Select(Converters.MapMuscleGroup).ToList(),
                p.trainingTemplates.Count,
                p.ShareToken,
                p.CopiedFromUserId.HasValue,
                p.CopiedFromUserId.HasValue ? authorNames.GetValueOrDefault(p.CopiedFromUserId.Value) : null
            ))
            .ToList();
    }
}

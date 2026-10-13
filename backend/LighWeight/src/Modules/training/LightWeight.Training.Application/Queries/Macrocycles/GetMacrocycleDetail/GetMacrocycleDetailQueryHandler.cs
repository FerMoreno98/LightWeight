using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.Macrocycles.GetMacrocycleDetail;

public sealed class GetMacrocycleDetailQueryHandler : IQueryHandler<GetMacrocycleDetailQuery, GetMacrocycleDetailResponse>
{
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly IProgramRepository _programRepository;

    public GetMacrocycleDetailQueryHandler(IMacrocycleRepository macrocycleRepository, IProgramRepository programRepository)
    {
        _macrocycleRepository = macrocycleRepository;
        _programRepository = programRepository;
    }

    public async Task<GetMacrocycleDetailResponse> HandleAsync(GetMacrocycleDetailQuery query, CancellationToken ct = default)
    {
        Macrocycle macrocycle = await _macrocycleRepository.GetByIdAsync(query.MacrocycleId)
            ?? throw new MacrocycleNotFoundException();
        if(macrocycle.UserId != query.UserId)
        {
            throw new UnauthorizedAccessException();
        }
        Dictionary<Guid, string> programNames = (await _programRepository.GetAllProgramsOfAUserAsync(query.UserId))
            .ToDictionary(p => p.Id, p => p.Name);

        return new GetMacrocycleDetailResponse
        (
            macrocycle.Id,
            macrocycle.Stage.ToString(),
            macrocycle.Comments,
            macrocycle.StartedAt,
            macrocycle.FinishedAt,
            macrocycle.Mesocycles
                .OrderBy(m => m.StartedAt)
                .Select(mesocycle => new MacrocycleMesocycleResponse
                (
                    mesocycle.Id,
                    mesocycle.ProgramId,
                    programNames.GetValueOrDefault(mesocycle.ProgramId, string.Empty),
                    mesocycle.StartedAt,
                    mesocycle.FinishedAt,
                    mesocycle.MotivationLevel,
                    mesocycle.Injuries,
                    mesocycle.Comments,
                    mesocycle.Microcycles.Count
                ))
                .ToList()
        );
    }
}

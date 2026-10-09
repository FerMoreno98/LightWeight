using LightWeight.shared.Mediator;
using LightWeight.Training.Application.Exceptions;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.Macrocycles.GetMacrocycleDetail;

public sealed class GetMacrocycleDetailQueryHandler : IQueryHandler<GetMacrocycleDetailQuery, GetMacrocycleDetailResponse>
{
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly IMesocycleRepository _mesocycleRepository;
    private readonly IMicrocycleRepository _microcycleRepository;
    private readonly IProgramRepository _programRepository;

    public GetMacrocycleDetailQueryHandler(IMacrocycleRepository macrocycleRepository, IMesocycleRepository mesocycleRepository, IMicrocycleRepository microcycleRepository, IProgramRepository programRepository)
    {
        _macrocycleRepository = macrocycleRepository;
        _mesocycleRepository = mesocycleRepository;
        _microcycleRepository = microcycleRepository;
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
        List<Mesocycle> mesocycles = await _mesocycleRepository.GetByMacrocycleIdAsync(macrocycle.Id);
        Dictionary<Guid, string> programNames = (await _programRepository.GetAllProgramsOfAUserAsync(query.UserId))
            .ToDictionary(p => p.Id, p => p.Name);

        var mesocycleResponses = new List<MacrocycleMesocycleResponse>();
        foreach(var mesocycle in mesocycles.OrderBy(m => m.StartedAt))
        {
            List<Microcycle> microcycles = await _microcycleRepository.GetByMesocycleIdAsync(mesocycle.Id);
            mesocycleResponses.Add(new MacrocycleMesocycleResponse
            (
                mesocycle.Id,
                mesocycle.ProgramId,
                programNames.GetValueOrDefault(mesocycle.ProgramId, string.Empty),
                mesocycle.StartedAt,
                mesocycle.FinishedAt,
                mesocycle.MotivationLevel,
                mesocycle.Injuries,
                mesocycle.Comments,
                microcycles.Count
            ));
        }

        return new GetMacrocycleDetailResponse
        (
            macrocycle.Id,
            macrocycle.Stage.ToString(),
            macrocycle.Comments,
            macrocycle.StartedAt,
            macrocycle.FinishedAt,
            mesocycleResponses
        );
    }
}

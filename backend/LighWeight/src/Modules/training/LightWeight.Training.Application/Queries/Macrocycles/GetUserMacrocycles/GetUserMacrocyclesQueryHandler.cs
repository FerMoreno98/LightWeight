using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.Macrocycles.GetUserMacrocycles;

public sealed class GetUserMacrocyclesQueryHandler : IQueryHandler<GetUserMacrocyclesQuery, List<GetUserMacrocyclesResponse>>
{
    private readonly IMacrocycleRepository _macrocycleRepository;
    private readonly IMesocycleRepository _mesocycleRepository;

    public GetUserMacrocyclesQueryHandler(IMacrocycleRepository macrocycleRepository, IMesocycleRepository mesocycleRepository)
    {
        _macrocycleRepository = macrocycleRepository;
        _mesocycleRepository = mesocycleRepository;
    }

    public async Task<List<GetUserMacrocyclesResponse>> HandleAsync(GetUserMacrocyclesQuery query, CancellationToken ct = default)
    {
        List<Macrocycle> macrocycles = await _macrocycleRepository.GetAllOfAUserAsync(query.UserId);
        List<Mesocycle> mesocycles = await _mesocycleRepository.GetAllOfAUserAsync(query.UserId);
        var mesocyclesPerMacrocycle = mesocycles
            .GroupBy(m => m.MacrocycleId)
            .ToDictionary(g => g.Key, g => g.Count());

        return macrocycles
            .OrderBy(m => m.IsFinished)
            .ThenByDescending(m => m.StartedAt)
            .Select(m => new GetUserMacrocyclesResponse
            (
                m.Id,
                m.Stage.ToString(),
                m.Comments,
                m.StartedAt,
                m.FinishedAt,
                mesocyclesPerMacrocycle.GetValueOrDefault(m.Id)
            ))
            .ToList();
    }
}

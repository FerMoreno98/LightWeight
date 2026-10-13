using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;

namespace LightWeight.Training.Application.Queries.Macrocycles.GetUserMacrocycles;

public sealed class GetUserMacrocyclesQueryHandler : IQueryHandler<GetUserMacrocyclesQuery, List<GetUserMacrocyclesResponse>>
{
    private readonly IMacrocycleRepository _macrocycleRepository;

    public GetUserMacrocyclesQueryHandler(IMacrocycleRepository macrocycleRepository)
    {
        _macrocycleRepository = macrocycleRepository;
    }

    public async Task<List<GetUserMacrocyclesResponse>> HandleAsync(GetUserMacrocyclesQuery query, CancellationToken ct = default)
    {
        List<Macrocycle> macrocycles = await _macrocycleRepository.GetAllOfAUserAsync(query.UserId);

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
                m.Mesocycles.Count
            ))
            .ToList();
    }
}

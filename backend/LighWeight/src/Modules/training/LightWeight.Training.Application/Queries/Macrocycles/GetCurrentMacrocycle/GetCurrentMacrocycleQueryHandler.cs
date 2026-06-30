using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;

namespace LightWeight.Training.Application.Queries.Macrocycles.GetCurrentMacrocycle;

public sealed class GetCurrentMacrocycleQueryHandler : IQueryHandler<GetCurrentMacrocycleQuery, GetMacrocycleResponse>
{
    public Task<GetMacrocycleResponse> HandleAsync(GetCurrentMacrocycleQuery query, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.Macrocycles.GetUserMacrocycles;

/// <summary>Macrocycles of the user: the active one first, then the finished ones from the most recent</summary>
public sealed record GetUserMacrocyclesQuery(Guid UserId) : IQuery<List<GetUserMacrocyclesResponse>>;

public sealed record GetUserMacrocyclesResponse
(
    Guid Id,
    string Stage,
    string? Comments,
    DateTime StartedAt,
    DateTime? FinishedAt,
    int MesocyclesCount
);

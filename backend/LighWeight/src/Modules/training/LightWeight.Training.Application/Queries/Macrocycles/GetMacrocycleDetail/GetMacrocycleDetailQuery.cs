using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.Macrocycles.GetMacrocycleDetail;

/// <summary>A macrocycle with its mesocycles, in the order they were started</summary>
public sealed record GetMacrocycleDetailQuery(Guid MacrocycleId, Guid UserId) : IQuery<GetMacrocycleDetailResponse>;

public sealed record GetMacrocycleDetailResponse
(
    Guid Id,
    string Stage,
    string? Comments,
    DateTime StartedAt,
    DateTime? FinishedAt,
    List<MacrocycleMesocycleResponse> Mesocycles
);

public sealed record MacrocycleMesocycleResponse
(
    Guid Id,
    Guid ProgramId,
    string ProgramName,
    DateTime StartedAt,
    DateTime? FinishedAt,
    int MotivationLevel,
    string? Injuries,
    string? Comments,
    int MicrocyclesCount
);

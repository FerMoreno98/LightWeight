using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Aggregates;

namespace LightWeight.Training.Application.Queries.Macrocycles.GetCurrentMacrocycle;

public sealed record GetCurrentMacrocycleQuery : IQuery<GetMacrocycleResponse>;

public record GetMacrocycleResponse
(
    Guid UserId,
    List<string> aimMuscleGroups,
    DateTime StartAt,
    DateTime EndAt,
    string TrainingStage,
    string Periodization,
    string? Comments
);
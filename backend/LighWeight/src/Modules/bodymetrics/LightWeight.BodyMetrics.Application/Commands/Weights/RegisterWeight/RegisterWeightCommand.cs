using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Commands.Weights.RegisterWeight;

/// <summary>Registers body weight (kg) of the user. Returns the id of the measurement</summary>
public sealed record RegisterWeightCommand
(
    Guid UserId,
    decimal Weight,
    DateTime MeasuredAt
) : ICommand<Guid>;

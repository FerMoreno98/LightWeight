using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Commands.Weights.CorrectWeight;

/// <summary>Replaces the values and/or the date of a measurement of body weight (kg) of the user</summary>
public sealed record CorrectWeightCommand
(
    Guid MeasurementId,
    Guid UserId,
    decimal Weight,
    DateTime MeasuredAt
) : ICommand;

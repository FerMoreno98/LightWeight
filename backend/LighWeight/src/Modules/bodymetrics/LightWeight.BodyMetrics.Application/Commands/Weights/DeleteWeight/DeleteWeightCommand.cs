using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Commands.Weights.DeleteWeight;

/// <summary>Deletes a measurement of body weight (kg) of the user</summary>
public sealed record DeleteWeightCommand(Guid MeasurementId, Guid UserId) : ICommand;

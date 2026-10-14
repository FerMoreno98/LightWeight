using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Commands.Perimeters.DeletePerimeters;

/// <summary>Deletes a measurement of perimeters (cm) of the user</summary>
public sealed record DeletePerimetersCommand(Guid MeasurementId, Guid UserId) : ICommand;

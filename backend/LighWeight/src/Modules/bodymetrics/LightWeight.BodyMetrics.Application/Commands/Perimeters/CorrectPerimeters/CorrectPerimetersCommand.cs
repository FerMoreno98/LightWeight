using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Commands.Perimeters.CorrectPerimeters;

/// <summary>Replaces the values and/or the date of a measurement of perimeters (cm) of the user</summary>
public sealed record CorrectPerimetersCommand
(
    Guid MeasurementId,
    Guid UserId,
    PerimeterValues Values,
    DateTime MeasuredAt
) : ICommand;

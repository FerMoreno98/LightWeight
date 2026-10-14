using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Commands.Perimeters.RegisterPerimeters;

/// <summary>Registers perimeters (cm) of the user. Returns the id of the measurement</summary>
public sealed record RegisterPerimetersCommand
(
    Guid UserId,
    PerimeterValues Values,
    DateTime MeasuredAt
) : ICommand<Guid>;

using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Commands.SkinFolds.RegisterSkinFolds;

/// <summary>Registers skinfolds (mm) of the user. Returns the id of the measurement</summary>
public sealed record RegisterSkinFoldsCommand
(
    Guid UserId,
    SkinFoldValues Values,
    DateTime MeasuredAt
) : ICommand<Guid>;

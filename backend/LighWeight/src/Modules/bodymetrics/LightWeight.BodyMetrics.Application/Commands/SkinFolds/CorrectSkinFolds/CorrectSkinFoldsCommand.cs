using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Commands.SkinFolds.CorrectSkinFolds;

/// <summary>Replaces the values and/or the date of a measurement of skinfolds (mm) of the user</summary>
public sealed record CorrectSkinFoldsCommand
(
    Guid MeasurementId,
    Guid UserId,
    SkinFoldValues Values,
    DateTime MeasuredAt
) : ICommand;

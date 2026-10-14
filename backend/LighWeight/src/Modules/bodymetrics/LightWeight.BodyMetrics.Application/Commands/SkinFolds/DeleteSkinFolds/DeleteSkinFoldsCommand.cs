using LightWeight.shared.Mediator;

namespace LightWeight.BodyMetrics.Application.Commands.SkinFolds.DeleteSkinFolds;

/// <summary>Deletes a measurement of skinfolds (mm) of the user</summary>
public sealed record DeleteSkinFoldsCommand(Guid MeasurementId, Guid UserId) : ICommand;

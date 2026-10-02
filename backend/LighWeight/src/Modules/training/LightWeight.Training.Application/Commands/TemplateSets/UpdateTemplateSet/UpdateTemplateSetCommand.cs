

using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TemplateSets.UpdateTemplateSet;

public sealed record UpdateTemplateSetCommand
(
    Guid TemplateSessionId,
    Guid UserId,
    Guid SetId,
    int Min,
    int Max,
    bool IsDropSet,
    bool IsMyoRep,
    bool IsCluster,
    decimal ExpectedRPE,
    List<string> AimMuscleGroups,
    Guid? SuperSetGroupId
) : ICommand;
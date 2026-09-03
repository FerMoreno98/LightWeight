using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TemplateSets.CreateTemplateSet;

public sealed record CreateTemplateSetCommand
(   
    Guid ExerciseId,
    Guid TemplateSessionId,
    Guid UserId,
    int Min,
    int Max,
    bool IsDropSet,
    bool IsMyoRep,
    bool IsCluster,
    int ExpectedRIR,
    List<string> AimMuscleGroups,
    Guid? SuperSetGroupId
) : ICommand;
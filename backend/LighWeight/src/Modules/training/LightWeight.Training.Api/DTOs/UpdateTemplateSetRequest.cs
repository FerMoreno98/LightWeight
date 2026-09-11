namespace LightWeight.Training.Api.DTOs;

public sealed record UpdateTemplateSetRequest
(
    Guid TemplateSessionId,
    Guid SetId,
    int Min,
    int Max,
    bool IsDropSet,
    bool IsMyoRep,
    bool IsCluster,
    int ExpectedRIR,
    List<string> AimMuscleGroups,
    Guid? SuperSetGroupId
);
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Api.DTOs;

public sealed record CreateTemplateSetRequest
(
    Guid ExerciseId,
    Guid TemplateSessionId,
    int Min,
    int Max,
    bool IsDropSet,
    bool IsMyoRep,
    bool IsCluster,
    int ExpectedRIR,
    List<string> AimMuscleGroups,
    Guid? SuperSetGroupId
);

namespace LightWeight.Training.Api.DTOs;

/// <param name="TemplateSetId">Planned set it covers, null for an extra set</param>
/// <param name="SuperSetGroupId">Superset it was performed in (planned, improvised or none)</param>
public sealed record RegisterSetRequest
(
    Guid ExerciseId,
    Guid? TemplateSetId,
    int Repetitions,
    decimal Weight,
    decimal RPE,
    bool IsBodyWeight,
    Guid? SuperSetGroupId
);

public sealed record AssignSuperSetRequest(List<Guid> SetIds, Guid? SuperSetGroupId);

public sealed record CreateTemplateSuperSetRequest(List<Guid> TemplateSetIds);

public sealed record UpdateSetRequest
(
    Guid ExerciseId,
    int Repetitions,
    decimal Weight,
    decimal RPE,
    bool IsBodyWeight
);

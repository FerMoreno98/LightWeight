using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Application.Queries.SetTemplates.GetSetsFromSessionTemplate;

/// <summary>Planned sets of a session of a template of the user</summary>
public sealed record GetSetsFromSessionTemplateQuery(Guid TemplateSessionId, Guid TrainingTemplateId, Guid UserId) : IQuery<List<GetSetsFromSessionTemplateResponse>>;

/// <param name="ExerciseName">Empty when the exercise no longer exists</param>
public sealed record GetSetsFromSessionTemplateResponse(
    Guid Id,
    Guid ExerciseId,
    string ExerciseName,
    int RepetitionRangeMin,
    int RepetitionRangeMax,
    decimal ExpectedRPE,
    string? Technique,
    Guid? SuperSetGroupId,
    IReadOnlyCollection<string> AimMuscleGroups
);

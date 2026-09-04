using LightWeight.shared.Mediator;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Application.Queries.SetTemplates.GetSetsFromSessionTemplate;

public sealed record GetSetsFromSessionTemplateQuery(Guid TemplateSessionId, Guid TrainingTemplateId) : IQuery<List<GetSetsFromSessionTemplateResponse>>;

public sealed record GetSetsFromSessionTemplateResponse(
    Guid ExerciseId,
    int RepetitionRangeMin,
    int RepetitionRangeMax,
    int ExpectedRIR,
    string? Technique,
    Guid? SuperSetGroupId,
    IReadOnlyCollection<string> AimMuscleGroups
);
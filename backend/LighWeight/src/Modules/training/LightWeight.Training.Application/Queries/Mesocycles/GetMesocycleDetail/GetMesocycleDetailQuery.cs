using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.Mesocycles.GetMesocycleDetail;

/// <summary>A mesocycle with its weeks and the templates of its program that can be used for the next week</summary>
public sealed record GetMesocycleDetailQuery(Guid MesocycleId, Guid UserId) : IQuery<GetMesocycleDetailResponse>;

public sealed record GetMesocycleDetailResponse
(
    Guid Id,
    Guid MacrocycleId,
    Guid ProgramId,
    string ProgramName,
    DateTime StartedAt,
    DateTime? FinishedAt,
    int MotivationLevel,
    string? Injuries,
    string? Comments,
    List<MesocycleMicrocycleResponse> Microcycles,
    List<MesocycleTemplateResponse> AvailableTemplates
);

/// <param name="TemplateName">Null when the template has been deleted</param>
public sealed record MesocycleMicrocycleResponse
(
    Guid Id,
    int WeekNumber,
    Guid TrainingTemplateId,
    string? TemplateName,
    int? DurationInDays
);

public sealed record MesocycleTemplateResponse
(
    Guid Id,
    string Name,
    int Order,
    int DurationInDays,
    string VolumeLandmark,
    string TrainingDistribution
);

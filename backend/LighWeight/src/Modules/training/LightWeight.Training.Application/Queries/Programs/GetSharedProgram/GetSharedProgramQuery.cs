using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.Programs.GetSharedProgram;

/// <summary>Read only preview of the program shared with the token, with its whole planning</summary>
public sealed record GetSharedProgramQuery(string ShareToken) : IQuery<GetSharedProgramResponse>;

/// <param name="AuthorName">Current name of the author. Null while it is not projected (see training_UserNames)</param>
public sealed record GetSharedProgramResponse
(
    string Name,
    string Periodization,
    IReadOnlyCollection<string> AimMuscleGroups,
    string? AuthorName,
    IReadOnlyCollection<GetSharedProgramTemplateResponse> Templates
);

public sealed record GetSharedProgramTemplateResponse
(
    string Name,
    int DurationInDays,
    string VolumeLandmark,
    string TrainingDistribution,
    IReadOnlyCollection<GetSharedProgramSessionResponse> Sessions
);

public sealed record GetSharedProgramSessionResponse
(
    string Name,
    IReadOnlyCollection<GetSharedProgramSetResponse> Sets
);

/// <param name="ExerciseId">Id in the global exercise catalog (lets the client group the consecutive sets of an exercise)</param>
/// <param name="ExerciseName">Empty when the exercise no longer exists</param>
public sealed record GetSharedProgramSetResponse
(
    Guid ExerciseId,
    string ExerciseName,
    int RepetitionRangeMin,
    int RepetitionRangeMax,
    decimal ExpectedRPE,
    string? Technique,
    Guid? SuperSetGroupId,
    IReadOnlyCollection<string> AimMuscleGroups
);

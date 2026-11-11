using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.Programs.GetUserPrograms;

public sealed record GetUserProgramsQuery(Guid UserId) : IQuery<List<GetUserProgramsResponse>>;

/// <param name="ShareToken">Token of the share link. Null while the program is not shared</param>
/// <param name="IsCopy">The program was copied from a program shared by another user</param>
/// <param name="CopiedFromAuthorName">Current name of the author of the original program. Null if it is not a copy or the name is not projected yet</param>
public sealed record GetUserProgramsResponse
(
    Guid Id,
    string Name,
    string Periodization,
    IReadOnlyCollection<string> AimMuscleGroups,
    int TrainingTemplatesCount,
    string? ShareToken,
    bool IsCopy,
    string? CopiedFromAuthorName
);

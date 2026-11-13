using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Queries.Setup.GetTrainingSetupStatus;

/// <summary>
/// How far the user is in setting up their training, worked out from what is saved (nothing extra is stored).
/// The onboarding wizard uses it to resume where the user left it, and the home to decide whether to offer it
/// </summary>
/// <param name="ProgramId">Program the wizard is working on. Null takes the most recent program of the user</param>
public sealed record GetTrainingSetupStatusQuery(Guid UserId, Guid? ProgramId) : IQuery<GetTrainingSetupStatusResponse>;

/// <summary>Next thing to do, in the order of the wizard</summary>
public static class TrainingSetupStep
{
    /// <summary>The user has no program</summary>
    public const string CreateProgram = "CreateProgram";
    /// <summary>The program has no templates</summary>
    public const string CreateTemplate = "CreateTemplate";
    /// <summary>The base template (the first one) has no sessions, or some session has no sets</summary>
    public const string FillSessions = "FillSessions";
    /// <summary>Only the base template exists. Optional: the block can already be launched (CanLaunch)</summary>
    public const string AddVolumeLevels = "AddVolumeLevels";
    /// <summary>Several templates and no active macrocycle</summary>
    public const string LaunchBlock = "LaunchBlock";
    /// <summary>There is an active macrocycle: the user is already training</summary>
    public const string Done = "Done";
}

/// <param name="Step">One of <see cref="TrainingSetupStep"/></param>
/// <param name="CanLaunch">True when the base template has sessions and all of them have sets</param>
/// <param name="IsFirstTime">True when the user has never had a macrocycle (finished ones count)</param>
/// <param name="ProgramId">Program the status refers to (null with CreateProgram)</param>
/// <param name="Templates">Templates of that program in their order; the first one is the base</param>
public sealed record GetTrainingSetupStatusResponse
(
    string Step,
    bool CanLaunch,
    bool IsFirstTime,
    Guid? ProgramId,
    string? ProgramName,
    List<SetupTemplateResponse> Templates
);

/// <param name="Sessions">Active sessions of the template</param>
/// <param name="SessionsWithSets">Active sessions with at least one planned set</param>
public sealed record SetupTemplateResponse
(
    Guid Id,
    string Name,
    string VolumeLandmark,
    int Sessions,
    int SessionsWithSets
);

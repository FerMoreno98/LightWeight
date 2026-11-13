using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Macrocycles.LaunchTrainingBlock;

/// <summary>
/// Starts training in one step: a new macrocycle, its first mesocycle following the program and the first week
/// following one of the program's templates. Everything is saved together, so it never leaves a macrocycle
/// without mesocycle. It is the last step of the onboarding wizard
/// </summary>
public sealed record LaunchTrainingBlockCommand
(
    Guid UserId,
    string TrainingStage,
    string? MacrocycleComments,
    Guid ProgramId,
    int MotivationLevel,
    string? Injuries,
    string? MesocycleComments,
    Guid TrainingTemplateId
) : ICommand<LaunchTrainingBlockResponse>;

/// <summary>Ids of the three levels just created</summary>
public sealed record LaunchTrainingBlockResponse(Guid MacrocycleId, Guid MesocycleId, Guid MicrocycleId);

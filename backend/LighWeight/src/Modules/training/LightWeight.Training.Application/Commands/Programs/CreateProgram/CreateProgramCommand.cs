using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Programs.CreateProgram;

public sealed record CreateProgramCommand
(
    Guid UserId,
    string Name,
    string Periodization,
    List<string> AimMuscleGroups
) : ICommand<Guid>;

using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Programs.ShareProgram;

/// <summary>Shares a program of the user. Returns its share token (the current one if it was already shared)</summary>
public sealed record ShareProgramCommand(Guid ProgramId, Guid UserId) : ICommand<string>;

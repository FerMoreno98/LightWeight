using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Programs.CopySharedProgram;

/// <summary>Copies the program shared with the token into the programs of the user. Returns the id of the copy</summary>
public sealed record CopySharedProgramCommand(string ShareToken, Guid UserId) : ICommand<Guid>;

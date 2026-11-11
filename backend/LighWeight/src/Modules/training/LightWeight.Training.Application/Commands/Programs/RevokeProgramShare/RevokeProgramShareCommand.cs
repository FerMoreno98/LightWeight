using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.Programs.RevokeProgramShare;

/// <summary>Stops sharing a program of the user: its link stops working. Copies already made are kept</summary>
public sealed record RevokeProgramShareCommand(Guid ProgramId, Guid UserId) : ICommand;

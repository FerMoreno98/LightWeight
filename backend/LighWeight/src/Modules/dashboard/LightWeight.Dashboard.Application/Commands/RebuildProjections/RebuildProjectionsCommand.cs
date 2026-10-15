using LightWeight.shared.Mediator;

namespace LightWeight.Dashboard.Application.Commands.RebuildProjections;

/// <summary>Empties the projections of the user and asks the source modules to publish their current state again</summary>
public sealed record RebuildProjectionsCommand(Guid UserId) : ICommand;

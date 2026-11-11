using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TemplateSets.ReorderTemplateSets;

/// <summary>Changes the order of the planned sets of a session of the user. Every active set, in the new order</summary>
public sealed record ReorderTemplateSetsCommand
(
    Guid TemplateSessionId,
    Guid UserId,
    List<Guid> TemplateSetIds
) : ICommand;

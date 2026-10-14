using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TemplateSuperSets.CreateTemplateSuperSet;

/// <summary>Joins planned sets of a session in a superset. Returns the superset id</summary>
public sealed record CreateTemplateSuperSetCommand
(
    Guid TemplateSessionId,
    Guid UserId,
    List<Guid> TemplateSetIds
) : ICommand<Guid>;

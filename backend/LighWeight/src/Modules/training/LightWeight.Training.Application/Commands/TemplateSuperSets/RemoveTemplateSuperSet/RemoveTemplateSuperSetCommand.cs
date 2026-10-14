using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TemplateSuperSets.RemoveTemplateSuperSet;

/// <summary>Separates a superset of a session: its sets become normal sets again</summary>
public sealed record RemoveTemplateSuperSetCommand
(
    Guid TemplateSessionId,
    Guid UserId,
    Guid SuperSetGroupId
) : ICommand;

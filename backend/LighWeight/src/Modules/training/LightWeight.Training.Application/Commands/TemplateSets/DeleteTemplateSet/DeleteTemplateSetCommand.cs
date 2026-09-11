using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TemplateSets.DeleteTemplateSet;

public sealed record DeleteTemplateSetCommand(Guid TemplateSetId, Guid UserId) : ICommand;
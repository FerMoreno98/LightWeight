using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TemplateSessions.CreateTemplateSession;
public sealed record CreateTemplateSessionCommand
(
    Guid TrainingTemplateId,
    Guid UserId,
    string Name
) : ICommand<Guid>;
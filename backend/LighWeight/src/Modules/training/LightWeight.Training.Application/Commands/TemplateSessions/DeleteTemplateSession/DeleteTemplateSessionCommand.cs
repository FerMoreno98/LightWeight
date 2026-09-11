

using LightWeight.shared.Mediator;

namespace LightWeight.Training.Application.Commands.TemplateSessions.DeleteTemplateSession;

public sealed record DeleteTemplateSessionCommand(Guid SessionId,Guid UserId) : ICommand;
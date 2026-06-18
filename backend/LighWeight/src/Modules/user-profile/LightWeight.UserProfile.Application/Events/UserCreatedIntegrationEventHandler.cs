using LightWeight.shared.Contracts.Auth;
using LightWeight.shared.Messaging;

namespace LightWeight.UserProfile.Application.Events;

public sealed class UserCreatedIntegrationEventHandler 
    : IIntegrationEventHandler<UserCreatedIntegrationEvent>
{
    // Por ahora no hace nada — el perfil se crea cuando el usuario completa el onboarding
    // Este es solo un evento de prueba para yo ver el flujo
    public Task HandleAsync(UserCreatedIntegrationEvent @event, CancellationToken ct)
        => Task.CompletedTask;
}
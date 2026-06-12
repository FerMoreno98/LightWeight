using FluentValidation;
using LightWeight.shared.Behavior;
using LightWeight.shared.Contracts.Auth;
using LightWeight.shared.Mediator;
using LightWeight.shared.Messaging;
using LightWeight.UserProfile.Application.Commands.CompleteProfile;
using LightWeight.UserProfile.Application.Events;
using Microsoft.Extensions.DependencyInjection;

namespace LightWeight.UserProfile.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();
        services.AddValidatorsFromAssembly(typeof(CompleteProfileCommand).Assembly);
        services.AddScoped(typeof(IPipelineBehavior<>), typeof(ValidationPipelineBehavior<>));

        services.AddScoped<ICommandHandler<CompleteProfileCommand>, CompleteProfileCommandHandler>();
        services.AddScoped<IIntegrationEventHandler<UserCreatedIntegrationEvent>, UserCreatedIntegrationEventHandler>();

        return services;
    }
}
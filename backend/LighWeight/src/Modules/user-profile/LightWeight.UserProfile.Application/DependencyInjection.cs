using FluentValidation;
using LightWeight.shared.Behavior;
using LightWeight.shared.Contracts.Auth;
using LightWeight.shared.Mediator;
using LightWeight.shared.Messaging;
using LightWeight.UserProfile.Application.Commands.ChangeTrainingStage;
using LightWeight.UserProfile.Application.Commands.CompleteProfile;
using LightWeight.UserProfile.Application.Commands.UpdateProfile;
using LightWeight.UserProfile.Application.Events;
using Microsoft.Extensions.DependencyInjection;

namespace LightWeight.UserProfile.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddUserProfileApplication(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();
        services.AddValidatorsFromAssembly(typeof(CompleteProfileCommand).Assembly);
        services.AddScoped(typeof(IPipelineBehavior<>), typeof(ValidationPipelineBehavior<>));

        services.AddScoped<ICommandHandler<CompleteProfileCommand>, CompleteProfileCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateProfileCommand>,UpdateProfileCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeTrainingStageCommand>,ChangeTrainingStageCommandHandler>();
        services.AddScoped<IIntegrationEventHandler<UserCreatedIntegrationEvent>, UserCreatedIntegrationEventHandler>();

        return services;
    }
}
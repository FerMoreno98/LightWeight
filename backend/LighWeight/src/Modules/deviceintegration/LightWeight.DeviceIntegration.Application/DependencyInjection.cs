using FluentValidation;
using LightWeight.DeviceIntegration.Application.Commands.FatSecret;
using LightWeight.DeviceIntegration.Application.Queries.FatSecret;
using LightWeight.DeviceIntegration.Application.Services;
using LightWeight.shared.Behavior;
using LightWeight.shared.Mediator;
using LightWeight.shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LightWeight.DeviceIntegration.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDeviceIntegrationApplication(this IServiceCollection services)
    {
        // Shared by every module: only registered if another module has not done it yet,
        // otherwise the validation pipeline would run once per registration
        services.TryAddScoped<IMediator, Mediator>();
        services.TryAddScoped<IEventPublisher, EventPublisher>();
        if (!services.Any(d => d.ServiceType == typeof(IPipelineBehavior<>) && d.ImplementationType == typeof(ValidationPipelineBehavior<>)))
            services.AddScoped(typeof(IPipelineBehavior<>), typeof(ValidationPipelineBehavior<>));
        if (!services.Any(d => d.ServiceType == typeof(IPipelineBehavior<,>) && d.ImplementationType == typeof(ValidationPipelineBehavior<,>)))
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Commands and queries
        services.AddScoped<ICommandHandler<StartFatSecretLinkingCommand, string>, StartFatSecretLinkingCommandHandler>();
        services.AddScoped<ICommandHandler<CompleteFatSecretLinkingCommand>, CompleteFatSecretLinkingCommandHandler>();
        services.AddScoped<ICommandHandler<UnlinkFatSecretCommand>, UnlinkFatSecretCommandHandler>();
        services.AddScoped<IQueryHandler<GetFatSecretConnectionQuery, FatSecretConnectionResponse>, GetFatSecretConnectionQueryHandler>();

        // Run by the background worker of Infrastructure, one user at a time
        services.AddScoped<FatSecretDiarySynchronizer>();
        return services;
    }
}

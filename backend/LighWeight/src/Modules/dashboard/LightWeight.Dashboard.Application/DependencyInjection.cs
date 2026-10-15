using FluentValidation;
using LightWeight.Dashboard.Application.Commands.RebuildProjections;
using LightWeight.Dashboard.Application.Projectors.BodyMetrics;
using LightWeight.shared.Behavior;
using LightWeight.shared.Contracts.BodyMetrics;
using LightWeight.shared.Mediator;
using LightWeight.shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LightWeight.Dashboard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDashboardApplication(this IServiceCollection services)
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

        // Commands
        services.AddScoped<ICommandHandler<RebuildProjectionsCommand>, RebuildProjectionsCommandHandler>();

        // Projectors: integration events of bodymetrics -> projections
        services.AddScoped<IIntegrationEventHandler<WeightRecordedIntegrationEvent>, WeightProjector>();
        services.AddScoped<IIntegrationEventHandler<WeightDeletedIntegrationEvent>, WeightProjector>();
        services.AddScoped<IIntegrationEventHandler<PerimetersRecordedIntegrationEvent>, PerimetersProjector>();
        services.AddScoped<IIntegrationEventHandler<PerimetersDeletedIntegrationEvent>, PerimetersProjector>();
        services.AddScoped<IIntegrationEventHandler<SkinFoldsRecordedIntegrationEvent>, SkinFoldsProjector>();
        services.AddScoped<IIntegrationEventHandler<SkinFoldsDeletedIntegrationEvent>, SkinFoldsProjector>();
        return services;
    }
}

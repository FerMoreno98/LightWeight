using FluentValidation;
using LightWeight.BodyMetrics.Application.Events;
using LightWeight.BodyMetrics.Application.Commands.Weights.RegisterWeight;
using LightWeight.BodyMetrics.Application.Commands.Weights.CorrectWeight;
using LightWeight.BodyMetrics.Application.Commands.Weights.DeleteWeight;
using LightWeight.BodyMetrics.Application.Queries.Weights.GetWeightHistory;
using LightWeight.BodyMetrics.Application.Commands.Perimeters.RegisterPerimeters;
using LightWeight.BodyMetrics.Application.Commands.Perimeters.CorrectPerimeters;
using LightWeight.BodyMetrics.Application.Commands.Perimeters.DeletePerimeters;
using LightWeight.BodyMetrics.Application.Queries.Perimeters.GetPerimetersHistory;
using LightWeight.BodyMetrics.Application.Commands.SkinFolds.RegisterSkinFolds;
using LightWeight.BodyMetrics.Application.Commands.SkinFolds.CorrectSkinFolds;
using LightWeight.BodyMetrics.Application.Commands.SkinFolds.DeleteSkinFolds;
using LightWeight.BodyMetrics.Application.Queries.SkinFolds.GetSkinFoldsHistory;
using LightWeight.BodyMetrics.Domain.Events;
using LightWeight.shared.Behavior;
using LightWeight.shared.Mediator;
using LightWeight.shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LightWeight.BodyMetrics.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddBodyMetricsApplication(this IServiceCollection services)
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
        services.AddScoped<ICommandHandler<RegisterWeightCommand, Guid>, RegisterWeightCommandHandler>();
        services.AddScoped<ICommandHandler<CorrectWeightCommand>, CorrectWeightCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteWeightCommand>, DeleteWeightCommandHandler>();
        services.AddScoped<IQueryHandler<GetWeightHistoryQuery, List<WeightResponse>>, GetWeightHistoryQueryHandler>();
        services.AddScoped<ICommandHandler<RegisterPerimetersCommand, Guid>, RegisterPerimetersCommandHandler>();
        services.AddScoped<ICommandHandler<CorrectPerimetersCommand>, CorrectPerimetersCommandHandler>();
        services.AddScoped<ICommandHandler<DeletePerimetersCommand>, DeletePerimetersCommandHandler>();
        services.AddScoped<IQueryHandler<GetPerimetersHistoryQuery, List<PerimetersResponse>>, GetPerimetersHistoryQueryHandler>();
        services.AddScoped<ICommandHandler<RegisterSkinFoldsCommand, Guid>, RegisterSkinFoldsCommandHandler>();
        services.AddScoped<ICommandHandler<CorrectSkinFoldsCommand>, CorrectSkinFoldsCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteSkinFoldsCommand>, DeleteSkinFoldsCommandHandler>();
        services.AddScoped<IQueryHandler<GetSkinFoldsHistoryQuery, List<SkinFoldsResponse>>, GetSkinFoldsHistoryQueryHandler>();

        // Domain events -> integration events. They are dispatched by the module's UnitOfWork after saving
        services.AddScoped<IDomainEventHandler<WeightRegisteredDomainEvent>, WeightDomainEventsHandler>();
        services.AddScoped<IDomainEventHandler<WeightCorrectedDomainEvent>, WeightDomainEventsHandler>();
        services.AddScoped<IDomainEventHandler<WeightDeletedDomainEvent>, WeightDomainEventsHandler>();
        services.AddScoped<IDomainEventHandler<PerimetersRegisteredDomainEvent>, PerimetersDomainEventsHandler>();
        services.AddScoped<IDomainEventHandler<PerimetersCorrectedDomainEvent>, PerimetersDomainEventsHandler>();
        services.AddScoped<IDomainEventHandler<PerimetersDeletedDomainEvent>, PerimetersDomainEventsHandler>();
        services.AddScoped<IDomainEventHandler<SkinFoldsRegisteredDomainEvent>, SkinFoldsDomainEventsHandler>();
        services.AddScoped<IDomainEventHandler<SkinFoldsCorrectedDomainEvent>, SkinFoldsDomainEventsHandler>();
        services.AddScoped<IDomainEventHandler<SkinFoldsDeletedDomainEvent>, SkinFoldsDomainEventsHandler>();
        return services;
    }
}

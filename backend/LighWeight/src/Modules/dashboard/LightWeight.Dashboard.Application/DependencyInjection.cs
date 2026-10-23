using FluentValidation;
using LightWeight.Dashboard.Application.Commands.RebuildProjections;
using LightWeight.Dashboard.Application.Projectors.BodyMetrics;
using LightWeight.Dashboard.Application.Projectors.Training;
using LightWeight.Dashboard.Application.Calculations.Training;
using LightWeight.Dashboard.Application.Queries.Training.GetExerciseProgress;
using LightWeight.Dashboard.Application.Queries.Training.GetExercisesProgress;
using LightWeight.Dashboard.Application.Queries.Training.GetSessionsPerformance;
using LightWeight.Dashboard.Application.Queries.Training.GetTrainingVolume;
using LightWeight.shared.Contracts.Training;
using LightWeight.Dashboard.Application.Queries.BodyMetrics.GetPerimetersEvolution;
using LightWeight.Dashboard.Application.Queries.BodyMetrics.GetSkinFoldsEvolution;
using LightWeight.Dashboard.Application.Queries.BodyMetrics.GetWeightEvolution;
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

        // Commands and queries
        services.AddScoped<ICommandHandler<RebuildProjectionsCommand>, RebuildProjectionsCommandHandler>();
        services.AddScoped<IQueryHandler<GetWeightEvolutionQuery, WeightEvolutionResponse>, GetWeightEvolutionQueryHandler>();
        services.AddScoped<IQueryHandler<GetPerimetersEvolutionQuery, List<PerimetersPoint>>, GetPerimetersEvolutionQueryHandler>();
        services.AddScoped<IQueryHandler<GetSkinFoldsEvolutionQuery, List<SkinFoldsPoint>>, GetSkinFoldsEvolutionQueryHandler>();

        // Projectors: integration events of bodymetrics -> projections
        services.AddScoped<IIntegrationEventHandler<WeightRecordedIntegrationEvent>, WeightProjector>();
        services.AddScoped<IIntegrationEventHandler<WeightDeletedIntegrationEvent>, WeightProjector>();
        services.AddScoped<IIntegrationEventHandler<PerimetersRecordedIntegrationEvent>, PerimetersProjector>();
        services.AddScoped<IIntegrationEventHandler<PerimetersDeletedIntegrationEvent>, PerimetersProjector>();
        services.AddScoped<IIntegrationEventHandler<SkinFoldsRecordedIntegrationEvent>, SkinFoldsProjector>();
        services.AddScoped<IIntegrationEventHandler<SkinFoldsDeletedIntegrationEvent>, SkinFoldsProjector>();

        // Training: projector of the finished sessions and the queries of the charts
        services.AddScoped<IIntegrationEventHandler<TrainingSessionFinishedIntegrationEvent>, TrainingSessionProjector>();
        services.AddScoped<IQueryHandler<GetTrainingVolumeQuery, List<WeeklyVolumePoint>>, GetTrainingVolumeQueryHandler>();
        services.AddScoped<IQueryHandler<GetSessionsPerformanceQuery, List<SessionPerformanceResponse>>, GetSessionsPerformanceQueryHandler>();
        services.AddScoped<IQueryHandler<GetExercisesProgressQuery, List<ExerciseProgressSummary>>, GetExercisesProgressQueryHandler>();
        services.AddScoped<IQueryHandler<GetExerciseProgressQuery, ExerciseProgressResponse?>, GetExerciseProgressQueryHandler>();
        return services;
    }
}

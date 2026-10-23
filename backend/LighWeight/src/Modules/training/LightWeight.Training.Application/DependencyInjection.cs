using FluentValidation;
using LightWeight.shared.Behavior;
using LightWeight.shared.Mediator;
using LightWeight.shared.Messaging;
using LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;
using LightWeight.Training.Application.Commands.Macrocycles.FinishMacrocycle;
using LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;
using LightWeight.Training.Application.Commands.Mesocycles.FinishMesocycle;
using LightWeight.Training.Application.Commands.Microcycles.CreateMicrocycle;
using LightWeight.Training.Application.Commands.Microcycles.FinishMicrocycle;
using LightWeight.Training.Application.Commands.Programs.CreateProgram;
using LightWeight.Training.Application.Commands.TemplateSessions.CreateTemplateSession;
using LightWeight.Training.Application.Commands.TemplateSessions.DeleteTemplateSession;
using LightWeight.Training.Application.Commands.TemplateSets.CreateTemplateSet;
using LightWeight.Training.Application.Commands.TemplateSets.DeleteTemplateSet;
using LightWeight.Training.Application.Commands.TemplateSets.UpdateTemplateSet;
using LightWeight.Training.Application.Commands.TemplateSuperSets.CreateTemplateSuperSet;
using LightWeight.Training.Application.Commands.TemplateSuperSets.RemoveTemplateSuperSet;
using LightWeight.Training.Application.Commands.TrainingSessions.AssignSuperSet;
using LightWeight.Training.Application.Commands.TrainingSessions.CreateTrainingSession;
using LightWeight.Training.Application.Commands.TrainingSessions.DiscardTrainingSession;
using LightWeight.Training.Application.Commands.TrainingSessions.FinishTrainingSession;
using LightWeight.Training.Application.Commands.TrainingSessions.RegisterSet;
using LightWeight.Training.Application.Commands.TrainingSessions.RemoveSet;
using LightWeight.Training.Application.Commands.TrainingSessions.UpdateSet;
using LightWeight.Training.Application.Queries.TrainingSessions.GetPreviousPerformance;
using LightWeight.Training.Application.Queries.TrainingSessions.GetSessionWorkout;
using LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;
using LightWeight.Training.Application.Commands.TrainingTemplates.DeleteTrainingTemplate;
using LightWeight.Training.Application.Commands.TrainingTemplates.DuplicateTrainingTemplate;
using LightWeight.Training.Application.Commands.TrainingTemplates.RenameTrainingTemplate;
using LightWeight.Training.Application.Queries.Exercises.GetAllExercises;
using LightWeight.Training.Application.Queries.Macrocycles.GetMacrocycleDetail;
using LightWeight.Training.Application.Queries.Macrocycles.GetUserMacrocycles;
using LightWeight.Training.Application.Queries.Mesocycles.GetMesocycleDetail;
using LightWeight.Training.Application.Queries.Microcycles.GetCurrentMicrocycle;
using LightWeight.Training.Application.Queries.Programs.GetUserPrograms;
using LightWeight.Training.Application.Queries.SessionTemplates.GetNumberOfSeriesPerGroupPerSession;
using LightWeight.Training.Application.Queries.SessionTemplates.GetTemplateSessionsFromTrainingTemplate;
using LightWeight.Training.Application.Queries.SetTemplates.GetSetsFromSessionTemplate;
using LightWeight.Training.Application.Queries.TrainingTemplates.GetUserTrainingTemplates;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Application.Events;
using LightWeight.Training.Domain.Events;
using LightWeight.shared.Contracts.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace LightWeight.Training.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTrainingApplication(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddValidatorsFromAssembly(typeof(CreateMacrocycleCommand).Assembly);
        services.AddScoped(typeof(IPipelineBehavior<>), typeof(ValidationPipelineBehavior<>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));

        services.AddScoped<ICommandHandler<CreateMacrocycleCommand, Guid>, CreateMacrocycleCommandHandler>();
        services.AddScoped<ICommandHandler<FinishMacrocycleCommand>, FinishMacrocycleCommandHandler>();
        services.AddScoped<ICommandHandler<CreateMesocycleCommand, Guid>, CreateMesocycleCommandHandler>();
        services.AddScoped<ICommandHandler<FinishMesocycleCommand>, FinishMesocycleCommandHandler>();
        services.AddScoped<ICommandHandler<CreateMicrocycleCommand, Guid>, CreateMicrocycleCommandHandler>();
        services.AddScoped<ICommandHandler<FinishMicrocycleCommand>, FinishMicrocycleCommandHandler>();
        services.AddScoped<IQueryHandler<GetCurrentMicrocycleQuery,GetCurrentMicrocycleResponse>,GetCurrentMicrocycleQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserMacrocyclesQuery,List<GetUserMacrocyclesResponse>>,GetUserMacrocyclesQueryHandler>();
        services.AddScoped<IQueryHandler<GetMacrocycleDetailQuery,GetMacrocycleDetailResponse>,GetMacrocycleDetailQueryHandler>();
        services.AddScoped<IQueryHandler<GetMesocycleDetailQuery,GetMesocycleDetailResponse>,GetMesocycleDetailQueryHandler>();
        services.AddScoped<ICommandHandler<CreateProgramCommand, Guid>, CreateProgramCommandHandler>();
        services.AddScoped<ICommandHandler<CreateTrainingSessionCommand, Guid>, CreateTrainingSessionCommandHandler>();
        services.AddScoped<ICommandHandler<RegisterSetCommand, Guid>, RegisterSetCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateSetCommand>, UpdateSetCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveSetCommand>, RemoveSetCommandHandler>();
        services.AddScoped<ICommandHandler<FinishTrainingSessionCommand, FinishTrainingSessionResponse>, FinishTrainingSessionCommandHandler>();
        services.AddScoped<ICommandHandler<DiscardTrainingSessionCommand>, DiscardTrainingSessionCommandHandler>();
        services.AddScoped<ICommandHandler<AssignSuperSetCommand>, AssignSuperSetCommandHandler>();
        services.AddScoped<ICommandHandler<CreateTemplateSuperSetCommand, Guid>, CreateTemplateSuperSetCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveTemplateSuperSetCommand>, RemoveTemplateSuperSetCommandHandler>();
        services.AddScoped<IQueryHandler<GetSessionWorkoutQuery, GetSessionWorkoutResponse>, GetSessionWorkoutQueryHandler>();
        services.AddScoped<IQueryHandler<GetPreviousPerformanceQuery, List<GetPreviousPerformanceResponse>>, GetPreviousPerformanceQueryHandler>();
        services.AddScoped<ICommandHandler<CreateTrainingTemplateCommand, Guid>, CreateTrainingTemplateCommandHandler>();
        services.AddScoped<ICommandHandler<CreateTemplateSessionCommand, Guid>,CreateTemplateSessionCommandHandler>();
        services.AddScoped<ICommandHandler<CreateTemplateSetCommand>,CreateTemplateSetCommandHandler>();
        services.AddScoped<IQueryHandler<GetAllExercisesQuery,List<GetAllExercisesResponse>>,GetAllExercisesQueryHandler>();
        services.AddScoped<IQueryHandler<GetTemplateSessionsFromTrainingTemplateQuery, List<GetTemplateSessionsFromTrainingTemplateResponse>>,GetTemplateSessionsFromTrainingTemplateQueryHandler>();
        services.AddScoped<IQueryHandler<GetSetsFromSessionTemplateQuery, List<GetSetsFromSessionTemplateResponse>>,GetSetsFromSessionTemplateQueryHandler>();
        services.AddScoped<IQueryHandler<GetNumberOfSeriesPerGroupPerSessionQuery,List<GetNumberOfSeriesPerGroupPerSessionResponse>>,GetNumberOfSeriesPerGroupPerSessionQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserTrainingTemplatesQuery,List<GetUserTrainingTemplatesResponse>>,GetUserTrainingTemplatesQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserProgramsQuery,List<GetUserProgramsResponse>>,GetUserProgramsQueryHandler>();
        services.AddScoped<ICommandHandler<DeleteTemplateSetCommand>,DeleteTemplateSetCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteTemplateSessionCommand>,DeleteTemplateSessionCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteTrainingTemplateCommand>,DeleteTrainingTemplateCommandHandler>();
        services.AddScoped<ICommandHandler<DuplicateTrainingTemplateCommand, Guid>,DuplicateTrainingTemplateCommandHandler>();
        services.AddScoped<ICommandHandler<RenameTrainingTemplateCommand>,RenameTrainingTemplateCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateTemplateSetCommand>,UpdateTemplateSetCommandHandler>();

        // Domain events -> integration events. They are dispatched by the module's UnitOfWork after saving
        services.AddScoped<TrainingSessionEventFactory>();
        services.AddScoped<IDomainEventHandler<TrainingSessionFinishedDomainEvent>, TrainingSessionDomainEventsHandler>();
        // The dashboard asks to publish again the finished sessions of a user when it rebuilds its projections
        services.AddScoped<IIntegrationEventHandler<RebuildRequestedIntegrationEvent>, RebuildRequestedIntegrationEventHandler>();
        return services;
    }
}

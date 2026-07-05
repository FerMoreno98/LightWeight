using FluentValidation;
using LightWeight.shared.Behavior;
using LightWeight.shared.Mediator;
using LightWeight.shared.Messaging;
using LightWeight.Training.Application.Commands.Macrocycles.CreateMacrocycle;
using LightWeight.Training.Application.Commands.Mesocycles.CreateMesocycle;
using LightWeight.Training.Application.Commands.Microcycles.CreateMicrocycle;
using LightWeight.Training.Application.Commands.TrainingSessions.CreateTrainingSession;
using LightWeight.Training.Application.Commands.TrainingTemplates.CreateTrainingTemplate;
using LightWeight.Training.Application.Queries.Macrocycles.GetCurrentMacrocycle;
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

        services.AddScoped<ICommandHandler<CreateMacrocycleCommand>, CreateMacrocycleCommandHandler>();
        services.AddScoped<ICommandHandler<CreateMesocycleCommand>, CreateMesocycleCommandHandler>();
        services.AddScoped<ICommandHandler<CreateMicrocycleCommand>, CreateMicrocycleCommandHandler>();
        services.AddScoped<ICommandHandler<CreateTrainingSessionCommand>, CreateTrainingSessionCommandHandler>();
        services.AddScoped<ICommandHandler<CreateTrainingTemplateCommand>, CreateTrainingTemplateCommandHandler>();
        services.AddScoped<IQueryHandler<GetCurrentMacrocycleQuery,GetMacrocycleResponse>,GetCurrentMacrocycleQueryHandler>();

        return services;
    }
}

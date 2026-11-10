using FluentValidation;
using LightWeight.shared.Behavior;
using LightWeight.shared.Contracts.Auth;
using LightWeight.shared.Contracts.Dashboard;
using LightWeight.shared.Mediator;
using LightWeight.shared.Messaging;

using LightWeight.UserProfile.Application.Commands.CompleteProfile;
using LightWeight.UserProfile.Application.Commands.UpdateProfile;
using LightWeight.UserProfile.Application.Events;
using LightWeight.UserProfile.Application.Queries.GetProfile;
using LightWeight.UserProfile.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LightWeight.UserProfile.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddUserProfileApplication(this IServiceCollection services)
    {
        // Shared by every module: only registered if another module has not done it yet,
        // otherwise the validation pipeline would run once per registration
        services.TryAddScoped<IMediator, Mediator>();
        services.TryAddScoped<IEventPublisher, EventPublisher>();
        if (!services.Any(d => d.ServiceType == typeof(IPipelineBehavior<>) && d.ImplementationType == typeof(ValidationPipelineBehavior<>)))
            services.AddScoped(typeof(IPipelineBehavior<>), typeof(ValidationPipelineBehavior<>));
        services.AddValidatorsFromAssembly(typeof(CompleteProfileCommand).Assembly);

        services.AddScoped<ICommandHandler<CompleteProfileCommand>, CompleteProfileCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateProfileCommand>,UpdateProfileCommandHandler>();
        services.AddScoped<IQueryHandler<GetProfileQuery, ProfileResponse>, GetProfileQueryHandler>();
        services.AddScoped<IIntegrationEventHandler<UserCreatedIntegrationEvent>, UserCreatedIntegrationEventHandler>();

        // Publishes the sex to the dashboard (fat percentage) and answers its rebuild
        services.AddScoped<IDomainEventHandler<UserCompletedDomainEvent>, UserProfileDomainEventsHandler>();
        services.AddScoped<IDomainEventHandler<UserProfileModifiedDomainEvent>, UserProfileDomainEventsHandler>();
        services.AddScoped<IIntegrationEventHandler<RebuildRequestedIntegrationEvent>, RebuildRequestedIntegrationEventHandler>();

        return services;
    }
}

using LightWeight.Nutrition.Application.Events;
using LightWeight.Nutrition.Domain.Events;
using LightWeight.shared.Contracts.Dashboard;
using LightWeight.shared.Contracts.DeviceIntegration;
using LightWeight.shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LightWeight.Nutrition.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNutritionApplication(this IServiceCollection services)
    {
        // Shared by every module: only registered if another module has not done it yet
        services.TryAddScoped<IEventPublisher, EventPublisher>();

        // The module has no commands nor queries: its data only arrives from FatSecret through DeviceIntegration
        services.AddScoped<IIntegrationEventHandler<FatSecretDiarySyncedIntegrationEvent>, FatSecretDiarySyncedIntegrationEventHandler>();

        // The dashboard asks to publish again the days of a user when it rebuilds its projections
        services.AddScoped<IIntegrationEventHandler<RebuildRequestedIntegrationEvent>, RebuildRequestedIntegrationEventHandler>();

        // Domain events -> integration events. They are dispatched by the module's UnitOfWork after saving
        services.AddScoped<IDomainEventHandler<DiaryDayRecordedDomainEvent>, DiaryDayDomainEventsHandler>();
        services.AddScoped<IDomainEventHandler<DiaryDayUpdatedDomainEvent>, DiaryDayDomainEventsHandler>();
        services.AddScoped<IDomainEventHandler<DiaryDayDeletedDomainEvent>, DiaryDayDomainEventsHandler>();
        return services;
    }
}

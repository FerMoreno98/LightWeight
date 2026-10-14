using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using LightWeight.BodyMetrics.Application;
using LightWeight.BodyMetrics.Domain.Repositories;
using LightWeight.BodyMetrics.Domain.Uow;
using LightWeight.BodyMetrics.Infrastructure.Persistence;
using LightWeight.BodyMetrics.Infrastructure.Persistence.Repositories;
using LightWeight.shared.Messaging;
using LightWeight.shared.Migrations;
using LightWeight.shared.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LightWeight.BodyMetrics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBodyMetricsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BodyMetricsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(configuration.GetConnectionString("DefaultConnection"))
                .ScanIn(typeof(DependencyInjection).Assembly).For.Migrations()
            )
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .Configure<SelectingProcessorAccessorOptions>(options =>
            {
                options.ProcessorId = "PostgreSQL";
            });

        services.AddScoped<IModuleMigrationManager>(sp =>
            new ModuleMigrationManager(
                sp.GetRequiredService<IMigrationRunner>(),
                "BodyMetrics"
            ));

        services.AddScoped<IBodyMetricsUnitOfWork, BodyMetricsUnitOfWork>();
        services.AddScoped<IWeightMeasurementRepository, WeightMeasurementRepository>();
        services.AddScoped<IPerimetersRepository, PerimetersRepository>();
        services.AddScoped<ISkinFoldsRepository, SkinFoldsRepository>();
        services.TryAddScoped<IEventDispatcher, EventDispatcher>();
        services.TryAddSingleton<IClock, SystemClock>();

        return services;
    }

    public static IServiceCollection AddBodyMetricsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBodyMetricsApplication();
        services.AddBodyMetricsInfrastructure(configuration);
        return services;
    }
}

using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using LightWeight.shared.Messaging;
using LightWeight.shared.Migrations;
using LightWeight.shared.Types;
using LightWeight.Training.Application;
using LightWeight.Training.Domain.Uow;
using LightWeight.Training.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LightWeight.Training.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTrainingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TrainingDbContext>(options =>
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
                "Training"
            ));

        services.AddScoped<ITrainingUnitOfWork, TrainingUnitOfWork>();
        services.AddScoped<IEventDispatcher, EventDispatcher>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }

    public static IServiceCollection AddTrainingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTrainingApplication();
        services.AddTrainingInfrastructure(configuration);
        return services;
    }
}

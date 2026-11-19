using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using LightWeight.Nutrition.Application;
using LightWeight.Nutrition.Domain.Repositories;
using LightWeight.Nutrition.Domain.Uow;
using LightWeight.Nutrition.Infrastructure.Persistence;
using LightWeight.Nutrition.Infrastructure.Persistence.Repositories;
using LightWeight.shared.Messaging;
using LightWeight.shared.Migrations;
using LightWeight.shared.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LightWeight.Nutrition.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNutritionInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NutritionDbContext>(options =>
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
                sp.GetRequiredService<ILogger<ModuleMigrationManager>>(),
                "Nutrition"
            ));

        services.AddScoped<INutritionUnitOfWork, NutritionUnitOfWork>();
        services.AddScoped<IDiaryDayRepository, DiaryDayRepository>();
        services.TryAddScoped<IEventDispatcher, EventDispatcher>();
        services.TryAddSingleton<IClock, SystemClock>();

        return services;
    }

    public static IServiceCollection AddNutritionModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddNutritionApplication();
        services.AddNutritionInfrastructure(configuration);
        return services;
    }
}

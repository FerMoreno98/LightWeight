using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using LightWeight.Dashboard.Application;
using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using LightWeight.Dashboard.Infrastructure.Persistence;
using LightWeight.shared.Migrations;
using LightWeight.shared.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LightWeight.Dashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDashboardInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DashboardDbContext>(options =>
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
                "Dashboard"
            ));

        services.AddScoped<IBodyMetricsProjectionStore, BodyMetricsProjectionStore>();
        services.TryAddSingleton<IClock, SystemClock>();

        return services;
    }

    public static IServiceCollection AddDashboardModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDashboardApplication();
        services.AddDashboardInfrastructure(configuration);
        return services;
    }
}

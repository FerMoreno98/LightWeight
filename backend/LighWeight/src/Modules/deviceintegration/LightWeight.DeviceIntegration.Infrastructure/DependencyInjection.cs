using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using LightWeight.DeviceIntegration.Application;
using LightWeight.DeviceIntegration.Application.Abstractions;
using LightWeight.DeviceIntegration.Domain.Repositories;
using LightWeight.DeviceIntegration.Domain.Uow;
using LightWeight.DeviceIntegration.Infrastructure.Background;
using LightWeight.DeviceIntegration.Infrastructure.FatSecret;
using LightWeight.DeviceIntegration.Infrastructure.Persistence;
using LightWeight.DeviceIntegration.Infrastructure.Security;
using LightWeight.shared.Migrations;
using LightWeight.shared.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LightWeight.DeviceIntegration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDeviceIntegrationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FatSecretOptions>(configuration.GetSection(FatSecretOptions.Section));

        services.AddDbContext<DeviceIntegrationDbContext>(options =>
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
                "DeviceIntegration"
            ));

        services.AddSingleton<TokenProtector>();
        services.AddScoped<IDeviceIntegrationUnitOfWork, DeviceIntegrationUnitOfWork>();
        services.AddScoped<IFatSecretConnectionRepository, FatSecretConnectionRepository>();
        // Without the default loggers: they write the full URL, and the query string carries oauth_token and the signature
        services.AddHttpClient<IFatSecretClient, FatSecretClient>(client => client.Timeout = TimeSpan.FromSeconds(30))
            .RemoveAllLoggers();

        // One queue, one consumer: the nightly run and the import after linking go through it
        services.AddSingleton<FatSecretSyncQueue>();
        services.AddSingleton<IFatSecretSyncQueue>(sp => sp.GetRequiredService<FatSecretSyncQueue>());
        services.AddHostedService<FatSecretSyncWorker>();
        services.AddHostedService<FatSecretNightlyScheduler>();

        services.TryAddSingleton<IClock, SystemClock>();

        return services;
    }

    public static IServiceCollection AddDeviceIntegrationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDeviceIntegrationApplication();
        services.AddDeviceIntegrationInfrastructure(configuration);
        return services;
    }
}

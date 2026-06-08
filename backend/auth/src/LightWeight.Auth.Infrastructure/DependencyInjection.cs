
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace LightWeight.Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(
                    // Esto son datos de pega, la idea es poner variables de entorno
                    "Host=;Port=;Database=;Username=;Password="
                )
                .ScanIn(typeof(DependencyInjection).Assembly).For.Migrations()
            )
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        return services;
    }
}
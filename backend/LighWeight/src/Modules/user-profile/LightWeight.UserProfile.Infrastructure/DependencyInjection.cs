using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using LightWeight.shared.Messaging;
using LightWeight.shared.Migrations;
using LightWeight.shared.Types;
using LightWeight.UserProfile.Application;
using LightWeight.UserProfile.Domain.Repository;
using LightWeight.UserProfile.Domain.Uow;
using LightWeight.UserProfile.Infrastructure.Persistence;
using LightWeight.UserProfile.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LightWeight.UserProfile.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddUserProfileInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UserProfileDbContext>(options =>
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
                options.ProcessorId = "PostgreSQL"; // Especificar el procesador
            });
                services.AddScoped<IModuleMigrationManager>(sp => 
                    new ModuleMigrationManager(
                sp.GetRequiredService<IMigrationRunner>(),
                "UserProfile"
            ));
            services.AddScoped<IUserRepository,UserRepository>();
            services.AddScoped<IUserProfileUnitOfWork,UnitOfWork>();
            services.AddScoped<IEventDispatcher, EventDispatcher>();
            services.AddSingleton<IClock, SystemClock>();

        return services;
    }
        public static IServiceCollection AddUserProfileModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddUserProfileApplication();          // handlers, validators (definido en Auth.Application)
        services.AddUserProfileInfrastructure(configuration); // dbcontext, repos, etc.
        return services;
    }
}
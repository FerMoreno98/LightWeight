
using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using LightWeight.Auth.Domain.Repository;
using LightWeight.Auth.Domain.Services;
using LightWeight.Auth.Domain.Uow;
using LightWeight.Auth.Infrastructure.Persistence;
using LightWeight.Auth.Infrastructure.Persistence.Repositories;
using LightWeight.Auth.Infrastructure.Services;
using LightWeight.shared.Messaging;
using LightWeight.shared.Migrations;
using LightWeight.shared.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LightWeight.Auth.Infrastructure;


public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options =>
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
                "Auth"
            ));
            services.AddScoped<IUserRepository,UserRepository>();
            services.AddScoped<IAuthUnitOfWork,AuthUnitOfWork>();
            services.AddScoped<ICodeHasher,CodeHasher>();
            services.AddHttpClient<IEmailSender, EmailSender>();
            services.AddScoped<IJwtTokenGenerator,TokenProvider>();
            services.AddSingleton<IClock, SystemClock>();
            services.AddScoped<IEventDispatcher, EventDispatcher>();

        return services;
    }
        public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthApplication();          // handlers, validators (definido en Auth.Application)
        services.AddAuthInfrastructure(configuration); // dbcontext, repos, etc.
        return services;
    }
}
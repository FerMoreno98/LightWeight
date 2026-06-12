
using FluentMigrator.Runner;
using LightWeight.Auth.Domain.Repository;
using LightWeight.Auth.Domain.Services;
using LightWeight.Auth.Infrastructure.Persistence;
using LightWeight.Auth.Infrastructure.Persistence.Repositories;
using LightWeight.Auth.Infrastructure.Services;
using LightWeight.shared.BuildingBlocks.Persistance;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LightWeight.Auth.Infrastructure;


public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(configuration.GetConnectionString("DefaultConnection"))
                .ScanIn(typeof(DependencyInjection).Assembly).For.Migrations()
            )
            .AddLogging(lb => lb.AddFluentMigratorConsole());
            services.AddScoped<IUserRepository,UserRepository>();
            services.AddScoped<IUnitOfWork,UnitOfWork>();
            services.AddScoped<ICodeHasher,CodeHasher>();
            services.AddHttpClient<IEmailSender, EmailSender>();
            services.AddScoped<IJwtTokenGenerator,TokenProvider>();
            services.AddSingleton<IClock, SystemClock>();
            services.AddScoped<IEventDispatcher, EventDispatcher>();

        return services;
    }
}
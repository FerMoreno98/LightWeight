using FluentMigrator.Runner;
using LightWeight.shared.BuildingBlocks.Persistance;
using LightWeight.shared.Messaging;
using LightWeight.shared.Types;
using LightWeight.UserProfile.Domain.Repository;
using LightWeight.UserProfile.Infrastructure.Persistence;
using LightWeight.UserProfile.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LightWeight.UserProfile.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UserProfileDbContext>(options =>
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
            services.AddScoped<IEventDispatcher, EventDispatcher>();
            services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
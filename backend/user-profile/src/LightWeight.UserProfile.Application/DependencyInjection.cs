using FluentValidation;
using LightWeight.shared.Behavior;
using LightWeight.shared.Mediator;
using LightWeight.UserProfile.Application.Commands.CompleteProfile;
using Microsoft.Extensions.DependencyInjection;

namespace LightWeight.UserProfile.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();
        services.AddValidatorsFromAssembly(typeof(CompleteProfileCommand).Assembly);
        services.AddScoped(typeof(IPipelineBehavior<>), typeof(ValidationPipelineBehavior<>));

        services.AddScoped<ICommandHandler<CompleteProfileCommand>, CompleteProfileCommandHandler>();


        return services;
    }
}
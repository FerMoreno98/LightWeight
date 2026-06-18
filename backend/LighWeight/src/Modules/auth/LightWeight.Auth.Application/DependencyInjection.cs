// LightWeight.Auth.Application/DependencyInjection.cs
using LightWeight.Auth.Application.Commands.LoginOtp.SendOtpCode;
using LightWeight.Auth.Application.Commands.LoginOtp.VerifyOtpCode;
using LightWeight.Auth.Application.Commands.LoginWithRefreshToken;
using LightWeight.Auth.Application.Commands.Logout;
using LightWeight.Auth.Application.Events;
using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Events;
using LightWeight.shared.Mediator;
using LightWeight.shared.Messaging;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();

        services.AddScoped<ICommandHandler<SendOtpCodeCommand>, SendOtpCodeCommandHandler>();
        services.AddScoped<ICommandHandler<VerifyOtpCodeCommand, OtpLoginResult>, VerifyOtpCodeCommandHandler>();
        services.AddScoped<ICommandHandler<LoginWithRefreshTokenCommand, LoginRefreshTokenResult>, LoginWithRefreshTokenCommandHandler>();
        services.AddScoped<ICommandHandler<LogoutCommand>, LogoutCommandHandler>();
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddScoped<IDomainEventHandler<UserCreatedDomainEvent>, UserCreatedDomainEventHandler>();


        return services;
    }
}
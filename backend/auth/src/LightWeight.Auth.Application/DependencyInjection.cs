// LightWeight.Auth.Application/DependencyInjection.cs
using LightWeight.Auth.Application.Commands.LoginOtp.SendOtpCode;
using LightWeight.Auth.Application.Commands.LoginOtp.VerifyOtpCode;
using LightWeight.Auth.Application.Commands.LoginWithRefreshToken;
using LightWeight.Auth.Application.Commands.Logout;
using LightWeight.Auth.Domain.Aggregates;
using LightWeight.shared.Mediator;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();

        services.AddScoped<ICommandHandler<SendOtpCodeCommand>, SendOtpCodeCommandHandler>();
        services.AddScoped<ICommandHandler<VerifyOtpCodeCommand, OtpLoginResult>, VerifyOtpCodeCommandHandler>();
        services.AddScoped<ICommandHandler<LoginWithRefreshTokenCommand, LoginRefreshTokenResult>, LoginWithRefreshTokenCommandHandler>();
        services.AddScoped<ICommandHandler<LogoutCommand>, LogoutCommandHandler>();

        return services;
    }
}
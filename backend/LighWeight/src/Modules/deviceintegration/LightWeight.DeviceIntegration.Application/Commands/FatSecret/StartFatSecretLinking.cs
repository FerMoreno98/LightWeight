using FluentValidation;
using LightWeight.DeviceIntegration.Application.Abstractions;
using LightWeight.DeviceIntegration.Application.Exceptions;
using LightWeight.DeviceIntegration.Domain.Aggregates;
using LightWeight.DeviceIntegration.Domain.Repositories;
using LightWeight.DeviceIntegration.Domain.Uow;
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;

namespace LightWeight.DeviceIntegration.Application.Commands.FatSecret;

/// <summary>Starts linking the FatSecret account of the user. Returns the FatSecret page to send the user to</summary>
public sealed record StartFatSecretLinkingCommand(Guid UserId) : ICommand<string>;

public sealed class StartFatSecretLinkingCommandValidator : AbstractValidator<StartFatSecretLinkingCommand>
{
    public StartFatSecretLinkingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class StartFatSecretLinkingCommandHandler : ICommandHandler<StartFatSecretLinkingCommand, string>
{
    private readonly IFatSecretConnectionRepository _connections;
    private readonly IDeviceIntegrationUnitOfWork _unitOfWork;
    private readonly IFatSecretClient _fatSecret;
    private readonly IClock _clock;

    public StartFatSecretLinkingCommandHandler(IFatSecretConnectionRepository connections, IDeviceIntegrationUnitOfWork unitOfWork,
        IFatSecretClient fatSecret, IClock clock)
    {
        _connections = connections;
        _unitOfWork = unitOfWork;
        _fatSecret = fatSecret;
        _clock = clock;
    }

    public async Task<string> HandleAsync(StartFatSecretLinkingCommand command, CancellationToken ct = default)
    {
        if (!_fatSecret.IsConfigured)
            throw new FatSecretNotConfiguredApplicationException();

        OAuthToken requestToken = await _fatSecret.GetRequestTokenAsync(ct);

        FatSecretConnection? connection = await _connections.GetByUserIdAsync(command.UserId, ct);
        if (connection is null)
        {
            connection = FatSecretConnection.Create(command.UserId);
            await _connections.AddAsync(connection, ct);
        }
        connection.StartLinking(requestToken.Token, requestToken.Secret, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(ct);

        return _fatSecret.GetAuthorizeUrl(requestToken.Token);
    }
}

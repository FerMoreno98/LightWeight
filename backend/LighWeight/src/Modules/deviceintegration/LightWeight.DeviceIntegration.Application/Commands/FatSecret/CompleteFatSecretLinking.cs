using FluentValidation;
using LightWeight.DeviceIntegration.Application.Abstractions;
using LightWeight.DeviceIntegration.Application.Exceptions;
using LightWeight.DeviceIntegration.Domain.Aggregates;
using LightWeight.DeviceIntegration.Domain.Exceptions;
using LightWeight.DeviceIntegration.Domain.Repositories;
using LightWeight.DeviceIntegration.Domain.Uow;
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;

namespace LightWeight.DeviceIntegration.Application.Commands.FatSecret;

/// <summary>
/// The user comes back from FatSecret with the authorized request token and the verifier. The request must be the
/// one this user started: that is what ties the FatSecret account to the logged-in user
/// </summary>
public sealed record CompleteFatSecretLinkingCommand(Guid UserId, string OAuthToken, string OAuthVerifier) : ICommand;

public sealed class CompleteFatSecretLinkingCommandValidator : AbstractValidator<CompleteFatSecretLinkingCommand>
{
    public CompleteFatSecretLinkingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.OAuthToken).NotEmpty().MaximumLength(256);
        RuleFor(x => x.OAuthVerifier).NotEmpty().MaximumLength(256);
    }
}

public sealed class CompleteFatSecretLinkingCommandHandler : ICommandHandler<CompleteFatSecretLinkingCommand>
{
    private readonly IFatSecretConnectionRepository _connections;
    private readonly IDeviceIntegrationUnitOfWork _unitOfWork;
    private readonly IFatSecretClient _fatSecret;
    private readonly IFatSecretSyncQueue _syncQueue;
    private readonly IClock _clock;

    public CompleteFatSecretLinkingCommandHandler(IFatSecretConnectionRepository connections, IDeviceIntegrationUnitOfWork unitOfWork,
        IFatSecretClient fatSecret, IFatSecretSyncQueue syncQueue, IClock clock)
    {
        _connections = connections;
        _unitOfWork = unitOfWork;
        _fatSecret = fatSecret;
        _syncQueue = syncQueue;
        _clock = clock;
    }

    public async Task HandleAsync(CompleteFatSecretLinkingCommand command, CancellationToken ct = default)
    {
        if (!_fatSecret.IsConfigured)
            throw new FatSecretNotConfiguredApplicationException();

        FatSecretConnection connection = await _connections.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NoPendingLinkingDomainException();
        // Checked before calling FatSecret: a token of another user must not be exchanged
        connection.EnsurePendingRequest(command.OAuthToken, _clock.UtcNow);

        OAuthToken accessToken = await _fatSecret.GetAccessTokenAsync(
            new OAuthToken(connection.RequestToken!, connection.RequestTokenSecret!), command.OAuthVerifier, ct);

        connection.CompleteLinking(command.OAuthToken, accessToken.Token, accessToken.Secret, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(ct);

        // The whole diary is imported in the background, not in this request
        _syncQueue.Enqueue(command.UserId);
    }
}

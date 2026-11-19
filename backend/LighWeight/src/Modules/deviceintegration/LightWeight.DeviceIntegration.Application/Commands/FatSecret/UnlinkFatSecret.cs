using FluentValidation;
using LightWeight.DeviceIntegration.Application.Exceptions;
using LightWeight.DeviceIntegration.Domain.Aggregates;
using LightWeight.DeviceIntegration.Domain.Repositories;
using LightWeight.DeviceIntegration.Domain.Uow;
using LightWeight.shared.Mediator;

namespace LightWeight.DeviceIntegration.Application.Commands.FatSecret;

/// <summary>
/// Forgets the FatSecret tokens of the user: the diary is not read any more. The days already imported stay in
/// Nutrition (they are the user's data, not FatSecret's)
/// </summary>
public sealed record UnlinkFatSecretCommand(Guid UserId) : ICommand;

public sealed class UnlinkFatSecretCommandValidator : AbstractValidator<UnlinkFatSecretCommand>
{
    public UnlinkFatSecretCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class UnlinkFatSecretCommandHandler : ICommandHandler<UnlinkFatSecretCommand>
{
    private readonly IFatSecretConnectionRepository _connections;
    private readonly IDeviceIntegrationUnitOfWork _unitOfWork;

    public UnlinkFatSecretCommandHandler(IFatSecretConnectionRepository connections, IDeviceIntegrationUnitOfWork unitOfWork)
    {
        _connections = connections;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(UnlinkFatSecretCommand command, CancellationToken ct = default)
    {
        FatSecretConnection connection = await _connections.GetByUserIdAsync(command.UserId, ct)
            ?? throw new FatSecretConnectionNotFoundApplicationException();
        _connections.Remove(connection);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}

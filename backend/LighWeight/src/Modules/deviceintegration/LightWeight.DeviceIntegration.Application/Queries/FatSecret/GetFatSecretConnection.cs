using LightWeight.DeviceIntegration.Application.Abstractions;
using LightWeight.DeviceIntegration.Domain.Aggregates;
using LightWeight.DeviceIntegration.Domain.Repositories;
using LightWeight.shared.Mediator;

namespace LightWeight.DeviceIntegration.Application.Queries.FatSecret;

public sealed record GetFatSecretConnectionQuery(Guid UserId) : IQuery<FatSecretConnectionResponse>;

/// <param name="Available">False when the server has no FatSecret credentials: the frontend hides the link button</param>
/// <param name="Status">NotLinked, Linked or Revoked (FatSecret rejected the token: link again)</param>
/// <param name="HistoryImported">False while the diary of a just linked account is being imported</param>
public sealed record FatSecretConnectionResponse(bool Available, string Status, DateTime? LinkedAt, bool HistoryImported);

public sealed class GetFatSecretConnectionQueryHandler : IQueryHandler<GetFatSecretConnectionQuery, FatSecretConnectionResponse>
{
    private readonly IFatSecretConnectionRepository _connections;
    private readonly IFatSecretClient _fatSecret;

    public GetFatSecretConnectionQueryHandler(IFatSecretConnectionRepository connections, IFatSecretClient fatSecret)
    {
        _connections = connections;
        _fatSecret = fatSecret;
    }

    public async Task<FatSecretConnectionResponse> HandleAsync(GetFatSecretConnectionQuery query, CancellationToken ct = default)
    {
        FatSecretConnection? connection = await _connections.GetByUserIdAsync(query.UserId, ct);

        // A linking in progress without an access token is still "not linked"
        string status = connection switch
        {
            { IsLinked: true } => "Linked",
            { AccessToken: not null } => "Revoked",
            _ => "NotLinked"
        };
        return new FatSecretConnectionResponse(
            _fatSecret.IsConfigured,
            status,
            connection?.AccessToken is null ? null : connection.LinkedAt,
            connection?.HistoryImportedAt is not null);
    }
}

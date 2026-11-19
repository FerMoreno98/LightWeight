using LightWeight.DeviceIntegration.Domain.Exceptions;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.DeviceIntegration.Domain.Aggregates;

/// <summary>
/// The link between a user and their FatSecret account (OAuth 1.0). One per user.
/// Linking takes two steps: <see cref="StartLinking"/> keeps the request token while the user authorizes
/// LightWeight in FatSecret, and <see cref="CompleteLinking"/> stores the access token. A pending request
/// does not touch the current access token, so linking again keeps the synchronization working until it finishes
/// </summary>
public sealed class FatSecretConnection : AggregateRoot<Guid>
{
    /// <summary>Time the user has to authorize LightWeight in FatSecret</summary>
    public static readonly TimeSpan PendingRequestLifetime = TimeSpan.FromMinutes(15);

    public Guid UserId { get; private set; }

    public string? AccessToken { get; private set; }
    public string? AccessTokenSecret { get; private set; }
    public DateTime? LinkedAt { get; private set; }
    /// <summary>When FatSecret rejected the access token: the user has to link the account again</summary>
    public DateTime? RevokedAt { get; private set; }
    /// <summary>Null until the whole diary has been imported once after linking</summary>
    public DateTime? HistoryImportedAt { get; private set; }

    public string? RequestToken { get; private set; }
    public string? RequestTokenSecret { get; private set; }
    public DateTime? RequestedAt { get; private set; }

    /// <summary>The diary can be read: there is an access token and FatSecret has not rejected it</summary>
    public bool IsLinked => AccessToken is not null && RevokedAt is null;

    private FatSecretConnection(Guid id) : base(id)
    {
    }

    /// <summary>First link of a user</summary>
    public static FatSecretConnection Create(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new UserIdEmptyDomainException();
        return new FatSecretConnection(Guid.CreateVersion7()) { UserId = userId };
    }

    /// <summary>Keeps the request token until the user comes back from FatSecret. Replaces a previous pending request</summary>
    public void StartLinking(string requestToken, string requestTokenSecret, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(requestToken) || string.IsNullOrWhiteSpace(requestTokenSecret))
            throw new InvalidOAuthTokenDomainException();
        RequestToken = requestToken;
        RequestTokenSecret = requestTokenSecret;
        RequestedAt = now;
    }

    /// <summary>
    /// Checks that the user comes back with the request token of this connection, still in time.
    /// Called before asking FatSecret for the access token, and again by <see cref="CompleteLinking"/>
    /// </summary>
    public void EnsurePendingRequest(string requestToken, DateTime now)
    {
        if (RequestToken is null || RequestedAt is null)
            throw new NoPendingLinkingDomainException();
        if (!string.Equals(RequestToken, requestToken, StringComparison.Ordinal))
            throw new LinkingRequestMismatchDomainException();
        if (now - RequestedAt.Value > PendingRequestLifetime)
            throw new LinkingRequestExpiredDomainException();
    }

    /// <summary>Stores the access token. The history is imported again: it may be another FatSecret account</summary>
    public void CompleteLinking(string requestToken, string accessToken, string accessTokenSecret, DateTime now)
    {
        EnsurePendingRequest(requestToken, now);
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(accessTokenSecret))
            throw new InvalidOAuthTokenDomainException();

        AccessToken = accessToken;
        AccessTokenSecret = accessTokenSecret;
        LinkedAt = now;
        RevokedAt = null;
        HistoryImportedAt = null;
        RequestToken = null;
        RequestTokenSecret = null;
        RequestedAt = null;
    }

    public void MarkHistoryImported(DateTime now)
    {
        if (!IsLinked)
            throw new FatSecretNotLinkedDomainException();
        HistoryImportedAt = now;
    }

    /// <summary>FatSecret rejected the access token (revoked by the user or expired). The token is kept until linking again</summary>
    public void MarkAccessRevoked(DateTime now)
    {
        if (!IsLinked)
            return;
        RevokedAt = now;
    }
}

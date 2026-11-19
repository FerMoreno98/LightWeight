using LightWeight.shared.Contracts.DeviceIntegration;

namespace LightWeight.DeviceIntegration.Application.Abstractions;

/// <summary>A token and its secret (OAuth 1.0): the request token while linking, the access token afterwards</summary>
public sealed record OAuthToken(string Token, string Secret);

/// <summary>The calls to FatSecret. Implemented in Infrastructure (OAuth 1.0, HMAC-SHA1)</summary>
public interface IFatSecretClient
{
    /// <summary>False when the consumer key, the secret or the callback are not configured</summary>
    bool IsConfigured { get; }

    /// <summary>Step 1: a request token. FatSecret will send the user back to the configured callback</summary>
    Task<OAuthToken> GetRequestTokenAsync(CancellationToken cancellationToken);

    /// <summary>Step 2: the FatSecret page where the user authorizes LightWeight</summary>
    string GetAuthorizeUrl(string requestToken);

    /// <summary>Step 3: exchanges the authorized request token for the access token of the user</summary>
    Task<OAuthToken> GetAccessTokenAsync(OAuthToken requestToken, string verifier, CancellationToken cancellationToken);

    /// <summary>
    /// The daily totals of the month that contains <paramref name="dayOfTheMonth"/>. Only the days with entries.
    /// Throws <see cref="FatSecretAccessRevokedException"/> when FatSecret rejects the access token
    /// </summary>
    Task<IReadOnlyList<FatSecretDiaryDay>> GetMonthAsync(OAuthToken accessToken, DateOnly dayOfTheMonth, CancellationToken cancellationToken);
}

/// <summary>FatSecret rejected the access token (error 9): the user revoked it or it expired</summary>
public sealed class FatSecretAccessRevokedException : Exception
{
    public FatSecretAccessRevokedException() : base("FatSecret rejected the access token")
    {

    }
}

/// <summary>Users whose diary has to be read. One consumer reads them one at a time</summary>
public interface IFatSecretSyncQueue
{
    void Enqueue(Guid userId);
}

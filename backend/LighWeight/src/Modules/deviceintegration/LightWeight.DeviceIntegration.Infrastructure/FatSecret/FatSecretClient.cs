using LightWeight.DeviceIntegration.Application.Abstractions;
using LightWeight.DeviceIntegration.Application.Exceptions;
using LightWeight.shared.Contracts.DeviceIntegration;
using LightWeight.shared.Types;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LightWeight.DeviceIntegration.Infrastructure.FatSecret;

/// <summary>
/// FatSecret over OAuth 1.0 (three-legged, HMAC-SHA1): the oauth_* parameters go signed in the query string or the
/// form body. Tokens and secrets are never logged
/// </summary>
internal sealed class FatSecretClient : IFatSecretClient
{
    internal const string RequestTokenUrl = "https://authentication.fatsecret.com/oauth/request_token";
    internal const string AuthorizeUrl = "https://authentication.fatsecret.com/oauth/authorize";
    internal const string AccessTokenUrl = "https://authentication.fatsecret.com/oauth/access_token";
    internal const string MonthUrl = "https://platform.fatsecret.com/rest/food-entries/month/v2";

    private readonly HttpClient _http;
    private readonly FatSecretOptions _options;
    private readonly IClock _clock;
    private readonly ILogger<FatSecretClient> _logger;

    public FatSecretClient(HttpClient http, IOptions<FatSecretOptions> options, IClock clock, ILogger<FatSecretClient> logger)
    {
        _http = http;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<OAuthToken> GetRequestTokenAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, string> parameters = Signed("POST", RequestTokenUrl, new() { ["oauth_callback"] = _options.CallbackUrl! }, null);
        using var request = new HttpRequestMessage(HttpMethod.Post, RequestTokenUrl) { Content = new FormUrlEncodedContent(parameters) };
        return await SendForTokenAsync(request, "request_token", cancellationToken);
    }

    public string GetAuthorizeUrl(string requestToken)
        => QueryHelpers.AddQueryString(AuthorizeUrl, "oauth_token", requestToken);

    public async Task<OAuthToken> GetAccessTokenAsync(OAuthToken requestToken, string verifier, CancellationToken cancellationToken)
    {
        Dictionary<string, string> parameters = Signed("GET", AccessTokenUrl,
            new() { ["oauth_token"] = requestToken.Token, ["oauth_verifier"] = verifier }, requestToken.Secret);
        using var request = new HttpRequestMessage(HttpMethod.Get, QueryHelpers.AddQueryString(AccessTokenUrl, parameters!));
        return await SendForTokenAsync(request, "access_token", cancellationToken);
    }

    public async Task<IReadOnlyList<FatSecretDiaryDay>> GetMonthAsync(OAuthToken accessToken, DateOnly dayOfTheMonth, CancellationToken cancellationToken)
    {
        Dictionary<string, string> parameters = Signed("GET", MonthUrl, new()
        {
            ["date"] = FatSecretMonthParser.ToDateInt(dayOfTheMonth).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["format"] = "json",
            ["oauth_token"] = accessToken.Token
        }, accessToken.Secret);

        using HttpResponseMessage response = await _http.GetAsync(QueryHelpers.AddQueryString(MonthUrl, parameters!), cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        // Errors come in the JSON body, sometimes with a 200 and sometimes not
        if (!response.IsSuccessStatusCode && !body.TrimStart().StartsWith('{'))
            throw new HttpRequestException($"FatSecret food_entries.get_month answered {(int)response.StatusCode}");
        return FatSecretMonthParser.Parse(body);
    }

    /// <summary>The request parameters plus the oauth_* ones and the signature</summary>
    private Dictionary<string, string> Signed(string method, string url, Dictionary<string, string> parameters, string? tokenSecret)
    {
        parameters["oauth_consumer_key"] = _options.ConsumerKey!;
        parameters["oauth_signature_method"] = "HMAC-SHA1";
        parameters["oauth_timestamp"] = new DateTimeOffset(_clock.UtcNow).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        parameters["oauth_nonce"] = OAuth1Signer.Nonce();
        parameters["oauth_version"] = "1.0";
        parameters["oauth_signature"] = OAuth1Signer.Sign(method, url, parameters, _options.ConsumerSecret!, tokenSecret);
        return parameters;
    }

    /// <summary>request_token and access_token answer form-encoded: oauth_token=…&amp;oauth_token_secret=…</summary>
    private async Task<OAuthToken> SendForTokenAsync(HttpRequestMessage request, string step, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                Dictionary<string, Microsoft.Extensions.Primitives.StringValues> values = QueryHelpers.ParseQuery(body);
                string? token = values.GetValueOrDefault("oauth_token").ToString();
                string? secret = values.GetValueOrDefault("oauth_token_secret").ToString();
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(secret))
                    return new OAuthToken(token, secret);
            }
            _logger.LogWarning("FatSecret {Step} failed with status {Status}", step, (int)response.StatusCode);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "FatSecret {Step} could not be reached", step);
        }
        throw new FatSecretUnavailableApplicationException();
    }
}

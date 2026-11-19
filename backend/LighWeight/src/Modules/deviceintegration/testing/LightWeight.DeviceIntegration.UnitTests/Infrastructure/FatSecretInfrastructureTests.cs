using System.Security.Cryptography;
using LightWeight.DeviceIntegration.Application.Abstractions;
using LightWeight.DeviceIntegration.Infrastructure.FatSecret;
using LightWeight.DeviceIntegration.Infrastructure.Security;
using LightWeight.shared.Contracts.DeviceIntegration;
using Microsoft.Extensions.Options;

namespace LightWeight.DeviceIntegration.UnitTests.Infrastructure;

public class FatSecretInfrastructureTests
{
    // ---------------------------------------------------------------- OAuth 1.0 signature

    /// <summary>The worked example of Twitter's "Creating a signature" guide, a common HMAC-SHA1 test vector</summary>
    [Fact]
    public void OAuth1Signer_MatchesTheKnownExample()
    {
        var parameters = new Dictionary<string, string>
        {
            ["status"] = "Hello Ladies + Gentlemen, a signed OAuth request!",
            ["include_entities"] = "true",
            ["oauth_consumer_key"] = "xvz1evFS4wEEPTGEFPHBog",
            ["oauth_nonce"] = "kYjzVBB8Y0ZFabxSWbWovY3uYSQ2pTgmZeNu2VS4cg",
            ["oauth_signature_method"] = "HMAC-SHA1",
            ["oauth_timestamp"] = "1318622958",
            ["oauth_token"] = "370773112-GmHxMAgYyLbNEtIKZeRNFsMKPR9EyMZeS9weJAEb",
            ["oauth_version"] = "1.0",
        };

        string signature = OAuth1Signer.Sign("POST", "https://api.twitter.com/1.1/statuses/update.json", parameters,
            "kAcSOqF21Fu85e7zjz7ZN2U4ZRhfV3WpwPAoE3Z7kBw", "LswwdoUaIvS8ltyTt5jkRh4J50vUPVVHtR2YPi5kE");

        Assert.Equal("hCtSmYh+iHYCEqBWrE7C7hYmtUk=", signature);
    }

    [Fact]
    public void OAuth1Signer_Encode_FollowsRfc3986()
    {
        Assert.Equal("Ladies%20%2B%20Gentlemen%21%2A%27%28%29", OAuth1Signer.Encode("Ladies + Gentlemen!*'()"));
        Assert.Equal("-._~", OAuth1Signer.Encode("-._~"));
    }

    // ---------------------------------------------------------------- food_entries.get_month

    [Fact]
    public void DateInt_IsDaysSince1970()
    {
        Assert.Equal(0, FatSecretMonthParser.ToDateInt(new DateOnly(1970, 1, 1)));
        Assert.Equal(20411, FatSecretMonthParser.ToDateInt(new DateOnly(2025, 11, 19)));
        Assert.Equal(new DateOnly(2025, 11, 19), FatSecretMonthParser.FromDateInt(20411));
    }

    [Fact]
    public void Parse_ManyDays_StringNumbers()
    {
        const string json = """
            {"month":{"day":[
              {"calories":"2450","carbohydrate":"280.50","date_int":"20410","fat":"75.20","protein":"165.00"},
              {"calories":"1980","carbohydrate":"190.00","date_int":"20411","fat":"60.00","protein":"150.30"}
            ],"from_date_int":"20393","to_date_int":"20422"}}
            """;

        IReadOnlyList<FatSecretDiaryDay> days = FatSecretMonthParser.Parse(json);

        Assert.Equal(2, days.Count);
        Assert.Equal(new FatSecretDiaryDay(new DateOnly(2025, 11, 18), 2450m, 165.00m, 280.50m, 75.20m), days[0]);
        Assert.Equal(150.30m, days[1].Protein);
    }

    [Fact]
    public void Parse_OneDay_ComesAsAnObject()
    {
        const string json = """{"month":{"day":{"calories":"2000","carbohydrate":"200","date_int":"20411","fat":"70","protein":"150"},"from_date_int":"20393","to_date_int":"20422"}}""";

        IReadOnlyList<FatSecretDiaryDay> days = FatSecretMonthParser.Parse(json);

        Assert.Single(days);
        Assert.Equal(new DateOnly(2025, 11, 19), days[0].Date);
    }

    /// <summary>The real answer of an empty month (checked against FatSecret on 2026-11-19)</summary>
    [Fact]
    public void Parse_NoEntries_IsEmpty()
    {
        Assert.Empty(FatSecretMonthParser.Parse("""{ "month": {"from_date_int": "20758", "to_date_int": "20787" }}"""));
    }

    [Theory]
    [InlineData("""{"month":null}""")]
    [InlineData("""{}""")]
    public void Parse_WithoutMonth_Throws(string json)
    {
        Assert.Throws<InvalidOperationException>(() => FatSecretMonthParser.Parse(json));
    }

    [Fact]
    public void Parse_Error9_IsRevoked()
    {
        Assert.Throws<FatSecretAccessRevokedException>(() =>
            FatSecretMonthParser.Parse("""{"error":{"code":9,"message":"Invalid access token"}}"""));
    }

    [Fact]
    public void Parse_OtherError_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            FatSecretMonthParser.Parse("""{"error":{"code":12,"message":"User is performing too many actions"}}"""));
    }

    // ---------------------------------------------------------------- Token encryption

    private static TokenProtector Protector(string key)
        => new(Options.Create(new FatSecretOptions { TokenEncryptionKey = key }));

    [Fact]
    public void TokenProtector_RoundTrip_AndEveryCiphertextIsDifferent()
    {
        TokenProtector protector = Protector(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

        string first = protector.Protect("access-token-secret");
        string second = protector.Protect("access-token-secret");

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("access-token-secret", first);
        Assert.Equal("access-token-secret", protector.Unprotect(first));
    }

    [Fact]
    public void TokenProtector_WithAnotherKey_CannotDecrypt()
    {
        string encrypted = Protector(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))).Protect("secret");

        Assert.ThrowsAny<CryptographicException>(() =>
            Protector(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))).Unprotect(encrypted));
    }

    [Fact]
    public void TokenProtector_KeyOfTheWrongSize_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Protector(Convert.ToBase64String(new byte[16])));
    }
}

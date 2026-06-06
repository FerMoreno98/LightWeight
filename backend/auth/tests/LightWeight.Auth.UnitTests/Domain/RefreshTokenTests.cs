using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Entities;
using Xunit;

namespace LightWeight.Auth.Tests.Domain;

public class RefreshTokenTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static readonly DateTime BaseTime =
        new(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Crea un RefreshToken a través de la cadena real del dominio:
    /// User -> DeviceToken -> RefreshToken
    /// </summary>
    private static RefreshToken CreateViaChain(
        string ip  = "192.168.1.1",
        DateTime? now = null)
    {
        var user   = User.Create("test@example.com",BaseTime);
        var device = user.RegisterDevice("device-001", "Mi Phone", "Android", now ?? BaseTime);
        return device.IssueRefreshToken(ip, now ?? BaseTime);
    }

    // ─── RefreshToken.Create ─────────────────────────────────────────────────────

    [Fact]
    public void Create_GeneratesNonEmptyId()
    {
        var token = RefreshToken.Create(Guid.CreateVersion7(), "192.168.1.1", BaseTime);

        Assert.NotEqual(Guid.Empty, token.Id);
    }

    [Fact]
    public void Create_TwoTokens_HaveDifferentIds()
    {
        var deviceId = Guid.CreateVersion7();

        var t1 = RefreshToken.Create(deviceId, "192.168.1.1", BaseTime);
        var t2 = RefreshToken.Create(deviceId, "192.168.1.1", BaseTime);

        Assert.NotEqual(t1.Id, t2.Id);
    }

    [Fact]
    public void Create_SetsDeviceTokenId()
    {
        var deviceId = Guid.CreateVersion7();

        var token = RefreshToken.Create(deviceId, "192.168.1.1", BaseTime);

        Assert.Equal(deviceId, token.DeviceTokenId);
    }

    [Fact]
    public void Create_SetsCreatedByIp()
    {
        var token = RefreshToken.Create(Guid.CreateVersion7(), "10.0.0.5", BaseTime);

        Assert.Equal("10.0.0.5", token.CreatedByIp);
    }

    [Fact]
    public void Create_SetsExpiresAtThirtyDaysFromNow()
    {
        var token = RefreshToken.Create(Guid.CreateVersion7(), "192.168.1.1", BaseTime);

        Assert.Equal(BaseTime.AddDays(30), token.ExpiresAt);
    }

    [Fact]
    public void Create_RevokedAtIsNull()
    {
        var token = RefreshToken.Create(Guid.CreateVersion7(), "192.168.1.1", BaseTime);

        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public void Create_ReplacedTokenByIsNull()
    {
        var token = RefreshToken.Create(Guid.CreateVersion7(), "192.168.1.1", BaseTime);

        Assert.Null(token.ReplacedTokenBy);
    }

    [Fact]
    public void Create_GeneratesNonEmptyTokenString()
    {
        var token = RefreshToken.Create(Guid.CreateVersion7(), "192.168.1.1", BaseTime);

        Assert.False(string.IsNullOrWhiteSpace(token.Token));
    }

    [Fact]
    public void Create_TokenString_IsValidBase64()
    {
        var token = RefreshToken.Create(Guid.CreateVersion7(), "192.168.1.1", BaseTime);

        var exception = Record.Exception(() => Convert.FromBase64String(token.Token));
        Assert.Null(exception);
    }

    [Fact]
    public void Create_TwoTokens_HaveDifferentTokenStrings()
    {
        var deviceId = Guid.CreateVersion7();

        var t1 = RefreshToken.Create(deviceId, "192.168.1.1", BaseTime);
        var t2 = RefreshToken.Create(deviceId, "192.168.1.1", BaseTime);

        Assert.NotEqual(t1.Token, t2.Token);
    }

    [Fact]
    public void Create_TokenString_HasExpectedBase64Length()
    {
        // 64 bytes en Base64 = 88 caracteres
        var token = RefreshToken.Create(Guid.CreateVersion7(), "192.168.1.1", BaseTime);

        Assert.Equal(88, token.Token.Length);
    }

    // ─── Integración: IssueRefreshToken vincula el DeviceTokenId correcto ────────

    [Fact]
    public void IssueRefreshToken_TokenDeviceTokenId_MatchesDevice()
    {
        var user   = User.Create("test@example.com",BaseTime);
        var device = user.RegisterDevice("device-001", "Mi Phone", "Android", BaseTime);

        var token = device.IssueRefreshToken("192.168.1.1", BaseTime);

        Assert.Equal(device.Id, token.DeviceTokenId);
    }

    [Fact]
    public void IssueRefreshToken_TokenIp_MatchesProvidedIp()
    {
        var token = CreateViaChain(ip: "172.16.0.1");

        Assert.Equal("172.16.0.1", token.CreatedByIp);
    }

    [Fact]
    public void IssueRefreshToken_ExpiresAt_IsThirtyDaysAfterNow()
    {
        var token = CreateViaChain(now: BaseTime);

        Assert.Equal(BaseTime.AddDays(30), token.ExpiresAt);
    }
}
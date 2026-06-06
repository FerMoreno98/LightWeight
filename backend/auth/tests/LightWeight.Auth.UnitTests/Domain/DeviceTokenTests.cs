using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Entities;
using Xunit;

namespace LightWeight.Auth.Tests.Domain.Aggregates;

public class DeviceTokenTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static readonly DateTime BaseTime =
        new(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Única vía válida para obtener un DeviceToken: a través del agregado User.
    /// </summary>
    private static DeviceToken CreateDevice(
        string   deviceIdentifier = "device-001",
        string   deviceName       = "Mi Phone",
        string   platform         = "Android",
        DateTime? now             = null,
        DateTime? revokedAt       = null)
    {
        var user = User.Create("test@example.com",now ?? BaseTime);
        return user.RegisterDevice(
            deviceIdentifier,
            deviceName,
            platform,
            now ?? BaseTime,
            revokedAt);
    }

    // ─── Propiedades iniciales ───────────────────────────────────────────────────

    [Fact]
    public void RegisterDevice_NewDevice_SetsDeviceIdentifier()
    {
        var device = CreateDevice(deviceIdentifier: "device-abc");

        Assert.Equal("device-abc", device.DeviceIdentifier);
    }

    [Fact]
    public void RegisterDevice_NewDevice_SetsDeviceName()
    {
        var device = CreateDevice(deviceName: "Samsung Galaxy");

        Assert.Equal("Samsung Galaxy", device.DeviceName);
    }

    [Fact]
    public void RegisterDevice_NewDevice_SetsPlatform()
    {
        var device = CreateDevice(platform: "iOS");

        Assert.Equal("iOS", device.Platform);
    }

    [Fact]
    public void RegisterDevice_NewDevice_SetsCreatedAt()
    {
        var device = CreateDevice(now: BaseTime);

        Assert.Equal(BaseTime, device.CreatedAt);
    }

    [Fact]
    public void RegisterDevice_NewDevice_SetsLastSeenAtEqualToCreatedAt()
    {
        var device = CreateDevice(now: BaseTime);

        Assert.Equal(device.CreatedAt, device.LastSeenAt);
    }

    [Fact]
    public void RegisterDevice_NewDevice_GeneratesNonEmptyId()
    {
        var device = CreateDevice();

        Assert.NotEqual(Guid.Empty, device.Id);
    }

    [Fact]
    public void RegisterDevice_NewDevice_UserIdMatchesUser()
    {
        var user   = User.Create("test@example.com", BaseTime);
        var device = user.RegisterDevice("device-001", "Mi Phone", "Android", BaseTime);

        Assert.Equal(user.Id, device.UserId);
    }

    [Fact]
    public void RegisterDevice_NewDevice_HasEmptyRefreshTokens()
    {
        var device = CreateDevice();

        Assert.Empty(device.RefreshTokens);
    }

    // ─── IsActive ───────────────────────────────────────────────────────────────

    [Fact]
    public void IsActive_WhenRevokedAtIsNull_ReturnsTrue()
    {
        var device = CreateDevice(revokedAt: null);

        Assert.True(device.IsActive);
    }

    [Fact]
    public void IsActive_WhenRevokedAtHasValue_ReturnsFalse()
    {
        var device = CreateDevice(revokedAt: BaseTime.AddDays(-1));

        Assert.False(device.IsActive);
    }

    [Fact]
    public void IsActive_WhenRevokedAtIsFutureDate_ReturnsFalse()
    {
        // IsActive solo comprueba si RevokedAt es null, no la fecha
        var device = CreateDevice(revokedAt: BaseTime.AddDays(30));

        Assert.False(device.IsActive);
    }

    // ─── UpdateLastSeen ──────────────────────────────────────────────────────────

    [Fact]
    public void UpdateLastSeen_SetsNewLastSeenAt()
    {
        var device = CreateDevice(now: BaseTime);
        var later  = BaseTime.AddHours(5);

        device.UpdateLastSeen(later);

        Assert.Equal(later, device.LastSeenAt);
    }

    [Fact]
    public void UpdateLastSeen_DoesNotChangeCreatedAt()
    {
        var device = CreateDevice(now: BaseTime);

        device.UpdateLastSeen(BaseTime.AddDays(10));

        Assert.Equal(BaseTime, device.CreatedAt);
    }

    [Fact]
    public void UpdateLastSeen_CalledMultipleTimes_KeepsLatestValue()
    {
        var device = CreateDevice(now: BaseTime);
        var last   = BaseTime.AddHours(10);

        device.UpdateLastSeen(BaseTime.AddHours(3));
        device.UpdateLastSeen(BaseTime.AddHours(7));
        device.UpdateLastSeen(last);

        Assert.Equal(last, device.LastSeenAt);
    }

    // ─── IssueRefreshToken ───────────────────────────────────────────────────────

    [Fact]
    public void IssueRefreshToken_AddsTokenToRefreshTokens()
    {
        var device = CreateDevice();

        device.IssueRefreshToken("192.168.1.1", BaseTime);

        Assert.Single(device.RefreshTokens);
    }

    [Fact]
    public void IssueRefreshToken_ReturnsNonNullToken()
    {
        var device = CreateDevice();

        var token = device.IssueRefreshToken("192.168.1.1", BaseTime);

        Assert.NotNull(token);
    }

    [Fact]
    public void IssueRefreshToken_ReturnedToken_HasCorrectDeviceId()
    {
        var device = CreateDevice();

        var token = device.IssueRefreshToken("192.168.1.1", BaseTime);

        Assert.Equal(device.Id, token.DeviceTokenId);
    }

    [Fact]
    public void IssueRefreshToken_ReturnedToken_HasCorrectIp()
    {
        var device = CreateDevice();

        var token = device.IssueRefreshToken("10.0.0.5", BaseTime);

        Assert.Equal("10.0.0.5", token.CreatedByIp);
    }

    // [Fact]
    // public void IssueRefreshToken_ReturnedToken_HasCorrectCreatedAt()
    // {
    //     var device = CreateDevice();

    //     var token = device.IssueRefreshToken("192.168.1.1", BaseTime);

    //     Assert.Equal(BaseTime, token.CreatedAt);
    // }

    [Fact]
    public void IssueRefreshToken_CalledTwice_AddsBothTokens()
    {
        var device = CreateDevice();

        device.IssueRefreshToken("192.168.1.1", BaseTime);
        device.IssueRefreshToken("192.168.1.2", BaseTime.AddMinutes(5));

        Assert.Equal(2, device.RefreshTokens.Count);
    }

    [Fact]
    public void IssueRefreshToken_EachToken_HasUniqueId()
    {
        var device = CreateDevice();

        var t1 = device.IssueRefreshToken("192.168.1.1", BaseTime);
        var t2 = device.IssueRefreshToken("192.168.1.2", BaseTime.AddMinutes(5));

        Assert.NotEqual(t1.Id, t2.Id);
    }

    // ─── RefreshTokens es read-only ──────────────────────────────────────────────

    [Fact]
    public void RefreshTokens_IsReadOnlyCollection()
    {
        var device = CreateDevice();

        Assert.IsAssignableFrom<IReadOnlyCollection<RefreshToken>>(device.RefreshTokens);
    }
}
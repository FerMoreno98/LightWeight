using LightWeight.Auth.Domain.Entities;
using LightWeight.Auth.Domain.Exceptions;
using Xunit;

namespace LightWeight.Auth.UnitTests.Domain;

public class OtpCodeTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────────

    private static readonly DateTime FixedNow =
        new(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private static readonly Guid FixedUserId = Guid.NewGuid();

    private static OtpCode BuildValidOtpCode(DateTime? now = null) =>
        OtpCode.Create("hashed:123456", FixedUserId, now ?? FixedNow);

    // ─── OtpCode.Create ──────────────────────────────────────────────────────────

    [Fact]
    public void Create_GeneratesNonEmptyId()
    {
        var otp = BuildValidOtpCode();

        Assert.NotEqual(Guid.Empty, otp.Id);
    }

    [Fact]
    public void Create_TwoOtpCodes_HaveDifferentIds()
    {
        var otp1 = BuildValidOtpCode();
        var otp2 = BuildValidOtpCode();

        Assert.NotEqual(otp1.Id, otp2.Id);
    }

    [Fact]
    public void Create_SetsHash()
    {
        var otp = BuildValidOtpCode();

        Assert.Equal("hashed:123456", otp.Hash);
    }

    [Fact]
    public void Create_SetsUserId()
    {
        var otp = BuildValidOtpCode();

        Assert.Equal(FixedUserId, otp.UserId);
    }

    [Fact]
    public void Create_SetsCreatedAtToNow()
    {
        var otp = BuildValidOtpCode(FixedNow);

        Assert.Equal(FixedNow, otp.CreatedAt);
    }

    [Fact]
    public void Create_SetsExpiresAtTenMinutesFromNow_ByDefault()
    {
        var otp = BuildValidOtpCode(FixedNow);

        Assert.Equal(FixedNow.AddMinutes(10), otp.ExpiresAt);
    }

    [Fact]
    public void Create_WithCustomTtl_SetsExpiresAtCorrectly()
    {
        var otp = OtpCode.Create("hashed:123456", FixedUserId, FixedNow, minutesTtl: 30);

        Assert.Equal(FixedNow.AddMinutes(30), otp.ExpiresAt);
    }

    [Fact]
    public void Create_UsedAtIsNull()
    {
        var otp = BuildValidOtpCode();

        Assert.Null(otp.UsedAt);
    }

    // ─── IsExpired ───────────────────────────────────────────────────────────────

    [Fact]
    public void IsExpired_WhenNowIsBeforeExpiresAt_ReturnsFalse()
    {
        var otp = BuildValidOtpCode(FixedNow);

        Assert.False(otp.IsExpired(FixedNow.AddMinutes(5)));
    }

    [Fact]
    public void IsExpired_WhenNowIsAfterExpiresAt_ReturnsTrue()
    {
        var otp = BuildValidOtpCode(FixedNow);

        Assert.True(otp.IsExpired(FixedNow.AddMinutes(11)));
    }

    [Fact]
    public void IsExpired_WhenNowEqualsExpiresAt_ReturnsFalse()
    {
        // now > ExpiresAt, no >=, en el límite exacto no está expirado
        var otp = BuildValidOtpCode(FixedNow);

        Assert.False(otp.IsExpired(FixedNow.AddMinutes(10)));
    }

    // ─── IsAlreadyUsed ───────────────────────────────────────────────────────────

    [Fact]
    public void IsAlreadyUsed_BeforeMarkAsUsed_ReturnsFalse()
    {
        var otp = BuildValidOtpCode();

        Assert.False(otp.IsAlreadyUsed());
    }

    [Fact]
    public void IsAlreadyUsed_AfterMarkAsUsed_ReturnsTrue()
    {
        var otp = BuildValidOtpCode();
        otp.MarkAsUsed(FixedNow);

        Assert.True(otp.IsAlreadyUsed());
    }

    // ─── MarkAsUsed ──────────────────────────────────────────────────────────────

    [Fact]
    public void MarkAsUsed_SetsUsedAt()
    {
        var otp = BuildValidOtpCode();

        otp.MarkAsUsed(FixedNow);

        Assert.NotNull(otp.UsedAt);
    }
}
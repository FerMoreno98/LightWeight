using LightWeight.Auth.Domain.Exceptions;
using LightWeight.Auth.Domain.Services;
using LightWeight.Auth.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace LightWeight.Auth.UnitTests.Domain.ValueObjects;

public class OtpCodeTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────────

    private readonly ICodeHasher _hasher = Substitute.For<ICodeHasher>();

    private static readonly DateTime FixedNow =
        new(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private OtpCode BuildValidOtpCode(DateTime? now = null)
    {
        _hasher.HashCode("123456").Returns("hashed:123456");
        var otp = OtpCode.Create("123456", _hasher, now ?? FixedNow);
        return otp;
    }

    // ─── OtpCode.Create ──────────────────────────────────────────────────────────

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
        _hasher.HashCode("123456").Returns("hashed:123456");

        var otp = OtpCode.Create("123456", _hasher, FixedNow, minutesTtl: 30);

        Assert.Equal(FixedNow.AddMinutes(30), otp.ExpiresAt);
    }

    [Fact]
    public void Create_SetsHashFromHasher()
    {
        var otp = BuildValidOtpCode();

        Assert.Equal("hashed:123456", otp.Hash);
    }

    [Fact]
    public void Create_CallsHashCodeOnHasher()
    {
        BuildValidOtpCode();

        _hasher.Received(1).HashCode("123456");
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
        // now > ExpiresAt, no >=, así que en el límite exacto no está expirado
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

        otp.MarkAsUsed();

        Assert.True(otp.IsAlreadyUsed());
    }

    // ─── MarkAsUsed ──────────────────────────────────────────────────────────────

    [Fact]
    public void MarkAsUsed_SetsUsedAt()
    {
        var otp = BuildValidOtpCode();

        otp.MarkAsUsed();

        Assert.NotNull(otp.UsedAt);
    }

    [Fact]
    public void MarkAsUsed_SetsUsedAtToApproximatelyNow()
    {
        var before = DateTime.UtcNow;
        var otp    = BuildValidOtpCode();

        otp.MarkAsUsed();

        var after = DateTime.UtcNow;
        Assert.InRange(otp.UsedAt!.Value, before, after);
    }

    // ─── Validate ────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithValidCode_ReturnsTrue()
    {
        var otp = BuildValidOtpCode(FixedNow);
        _hasher.Verify("123456", "hashed:123456").Returns(true);

        var result = otp.Validate("123456", _hasher, FixedNow);

        Assert.True(result);
    }

    [Fact]
    public void Validate_WhenExpired_ThrowsOtpCodeExpiredException()
    {
        var otp = BuildValidOtpCode(FixedNow);

        Assert.Throws<OtpCodeExpiredException>(
            () => otp.Validate("123456", _hasher, FixedNow.AddMinutes(20)));
    }

    [Fact]
    public void Validate_WhenAlreadyUsed_ThrowsOtpCodeAlreadyUsedException()
    {
        var otp = BuildValidOtpCode(FixedNow);
        otp.MarkAsUsed();

        Assert.Throws<OtpCodeAlreadyUsedException>(
            () => otp.Validate("123456", _hasher, FixedNow));
    }

    [Fact]
    public void Validate_WhenCodeIsWrong_ThrowsInvalidOtpCodeException()
    {
        var otp = BuildValidOtpCode(FixedNow);
        _hasher.Verify("wrong", "hashed:123456").Returns(false);

        Assert.Throws<InvalidOtpCodeException>(
            () => otp.Validate("wrong", _hasher, FixedNow));
    }

    [Fact]
    public void Validate_WhenExpired_DoesNotCheckAlreadyUsedOrHash()
    {
        // La validación debe cortocircuitar en IsExpired sin llegar al hasher
        var otp = BuildValidOtpCode(FixedNow);

        Assert.Throws<OtpCodeExpiredException>(
            () => otp.Validate("123456", _hasher, FixedNow.AddMinutes(20)));

        _hasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void Validate_WhenAlreadyUsed_DoesNotCheckHash()
    {
        // La validación debe cortocircuitar en IsAlreadyUsed sin llegar al hasher
        var otp = BuildValidOtpCode(FixedNow);
        otp.MarkAsUsed();

        Assert.Throws<OtpCodeAlreadyUsedException>(
            () => otp.Validate("123456", _hasher, FixedNow));

        _hasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }
}
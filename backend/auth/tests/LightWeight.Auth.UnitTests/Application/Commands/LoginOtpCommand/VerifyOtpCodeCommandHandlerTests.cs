using LightWeight.Auth.Application.Commands.LoginOtp.VerifyOtpCode;
using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Exceptions;
using LightWeight.Auth.Domain.Repository;
using LightWeight.Auth.Domain.Services;
using LightWeight.Auth.Domain.ValueObjects;
using LightWeight.shared.BuildingBlocks.Persistance;
using LightWeight.shared.Types;
using NSubstitute;
using Xunit;

namespace LightWeight.Auth.Tests.Application.Commands;

public class VerifyOtpCodeCommandHandlerTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────────

    private readonly IUserRepository    _userRepository    = Substitute.For<IUserRepository>();
    private readonly ICodeHasher        _hashService       = Substitute.For<ICodeHasher>();
    private readonly IClock             _clock             = Substitute.For<IClock>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IUnitOfWork        _uow               = Substitute.For<IUnitOfWork>();

    private static readonly DateTime FixedNow =
        new(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private VerifyOtpCodeCommandHandler BuildHandler() =>
        new(_userRepository, _hashService, _clock, _jwtTokenGenerator, _uow);

    private static VerifyOtpCodeCommand BuildCommand(string email = "user@example.com") =>
        new(
            Code:             "123456",
            Ip:               "192.168.1.1",
            DeviceIdentifier: "device-001",
            DeviceName:       "Mi Phone",
            Platform:         "Android",
            Email:            email
        );

    /// <summary>
    /// Construye un OtpCode válido y configura el hasher.
    /// Siempre asignar a variable local ANTES de pasarlo a cualquier .Returns().
    /// </summary>
    private OtpCode BuildValidOtpCode()
    {
        _hashService.HashCode("123456").Returns("hashed:123456");
        _hashService.Verify("123456", "hashed:123456").Returns(true);
        return OtpCode.Create("123456", _hashService, FixedNow);
    }

    private void SetupValidOtp()
    {
        var otp = BuildValidOtpCode();
        _userRepository.GetHashedCodeAsync("user@example.com").Returns(otp);
    }

    public VerifyOtpCodeCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _jwtTokenGenerator.GenerateToken(Arg.Any<User>()).Returns("jwt-token");
    }

    // ─── Validación del OTP ──────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenOtpIsExpired_ThrowsOtpCodeExpiredException()
    {
        var expired = OtpCode.Create("123456", _hashService, FixedNow.AddMinutes(-20));
        _userRepository.GetHashedCodeAsync("user@example.com").Returns(expired);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<OtpCodeExpiredException>(
            () => handler.HandleAsync(BuildCommand()));
    }

    [Fact]
    public async Task HandleAsync_WhenOtpCodeIsWrong_ThrowsInvalidOtpCodeException()
    {
        _hashService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var otp = OtpCode.Create("123456", _hashService, FixedNow);
        _userRepository.GetHashedCodeAsync("user@example.com").Returns(otp);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<InvalidOtpCodeException>(
            () => handler.HandleAsync(BuildCommand()));
    }

    [Fact]
    public async Task HandleAsync_WhenOtpAlreadyUsed_ThrowsOtpCodeAlreadyUsedException()
    {
        var otp = BuildValidOtpCode();
        otp.MarkAsUsed();
        _userRepository.GetHashedCodeAsync("user@example.com").Returns(otp);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<OtpCodeAlreadyUsedException>(
            () => handler.HandleAsync(BuildCommand()));
    }

    [Fact]
    public async Task HandleAsync_ValidOtp_MarksCodeAsUsed()
    {
        var otp = BuildValidOtpCode();
        _userRepository.GetHashedCodeAsync("user@example.com").Returns(otp);
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        await _userRepository.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        Assert.True(otp.IsAlreadyUsed());
    }

    // ─── Usuario existente ───────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenUserExists_ReturnsIsExistingUserTrue()
    {
        SetupValidOtp();
        var user = User.Create("user@example.com", FixedNow);
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        var result = await handler.HandleAsync(BuildCommand());

        Assert.True(result.IsExistingUser);
    }

    [Fact]
    public async Task HandleAsync_WhenUserExists_ReturnsJwtToken()
    {
        SetupValidOtp();
        var user = User.Create("user@example.com", FixedNow);
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        var result = await handler.HandleAsync(BuildCommand());

        Assert.Equal("jwt-token", result.AccessToken);
    }

    [Fact]
    public async Task HandleAsync_WhenUserExists_ReturnsNonEmptyRefreshToken()
    {
        SetupValidOtp();
        var user = User.Create("user@example.com", FixedNow);
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        var result = await handler.HandleAsync(BuildCommand());

        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
    }

    [Fact]
    public async Task HandleAsync_WhenUserExists_DoesNotCallAddAsync()
    {
        SetupValidOtp();
        var user = User.Create("user@example.com", FixedNow);
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenUserExists_RegistersDeviceWithCommandData()
    {
        SetupValidOtp();
        var user    = User.Create("user@example.com", FixedNow);
        var command = BuildCommand();
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(command);

        var device = user.DeviceTokens.Single();
        Assert.Equal(command.DeviceIdentifier, device.DeviceIdentifier);
        Assert.Equal(command.DeviceName,       device.DeviceName);
        Assert.Equal(command.Platform,         device.Platform);
    }

    [Fact]
    public async Task HandleAsync_WhenUserExists_IssuedRefreshTokenHasCorrectIp()
    {
        SetupValidOtp();
        var user    = User.Create("user@example.com", FixedNow);
        var command = BuildCommand();
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(command);

        var refreshToken = user.DeviceTokens.Single().RefreshTokens.Single();
        Assert.Equal(command.Ip, refreshToken.CreatedByIp);
    }

    [Fact]
    public async Task HandleAsync_WhenUserExists_CallsSaveChanges()
    {
        SetupValidOtp();
        var user = User.Create("user@example.com", FixedNow);
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── Usuario nuevo ───────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ReturnsIsExistingUserFalse()
    {
        SetupValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        var handler = BuildHandler();

        var result = await handler.HandleAsync(BuildCommand());

        Assert.False(result.IsExistingUser);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ReturnsJwtToken()
    {
        SetupValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        var handler = BuildHandler();

        var result = await handler.HandleAsync(BuildCommand());

        Assert.Equal("jwt-token", result.AccessToken);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ReturnsNonEmptyRefreshToken()
    {
        SetupValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        var handler = BuildHandler();

        var result = await handler.HandleAsync(BuildCommand());

        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_CallsAddAsync()
    {
        SetupValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        await _userRepository.Received(1)
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_AddsUserWithCorrectEmail()
    {
        SetupValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        User? captured = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => captured = u),
            Arg.Any<CancellationToken>());
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        Assert.NotNull(captured);
        Assert.Equal("user@example.com", captured!.Email);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_CallsSaveChanges()
    {
        SetupValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_RegistersDeviceWithCommandData()
    {
        SetupValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        User? captured = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => captured = u),
            Arg.Any<CancellationToken>());
        var command = BuildCommand();
        var handler = BuildHandler();

        await handler.HandleAsync(command);

        var device = captured!.DeviceTokens.Single();
        Assert.Equal(command.DeviceIdentifier, device.DeviceIdentifier);
        Assert.Equal(command.DeviceName,       device.DeviceName);
        Assert.Equal(command.Platform,         device.Platform);
    }

    // ─── Propagación del CancellationToken ──────────────────────────────────────

    [Fact]
    public async Task HandleAsync_PropagatesCancellationTokenToSaveChanges()
    {
        SetupValidOtp();
        var user = User.Create("user@example.com", FixedNow);
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var cts     = new CancellationTokenSource();
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(), cts.Token);

        await _uow.Received(1).SaveChangesAsync(cts.Token);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_PropagatesCancellationTokenToAddAsync()
    {
        SetupValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        var cts     = new CancellationTokenSource();
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(), cts.Token);

        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), cts.Token);
    }

    // ─── Uso del reloj inyectado ─────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_UsesClockUtcNow()
    {
        SetupValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        _ = _clock.Received().UtcNow;
    }
}
using LightWeight.Auth.Application.Commands.LoginOtp.SendOtpCode;
using LightWeight.Auth.Application.Commands.LoginOtp.VerifyOtpCode;
using LightWeight.Auth.Application.Exceptions;
using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Entities;
using LightWeight.Auth.Domain.Repository;
using LightWeight.Auth.Domain.Services;
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

    private static VerifyOtpCodeCommand BuildCommand(
        string code  = "123456",
        string email = "user@example.com") =>
        new(
            Code:             code,
            Ip:               "192.168.1.1",
            DeviceIdentifier: "device-001",
            DeviceName:       "Mi Phone",
            Platform:         "Android",
            Email:            email
        );

    /// <summary>
    /// Crea un User con un OtpCode válido (no usado, no expirado) cuyo hash
    /// verifica correctamente el código "123456".
    /// </summary>
    private User BuildUserWithValidOtp()
    {
        _hashService.Verify("123456", "hashed:123456").Returns(true);
        var user    = User.Create("user@example.com", FixedNow);
        var otpCode = OtpCode.Create("hashed:123456", user.Id, FixedNow);
        user.AddOtpCode(otpCode);
        return user;
    }

    public VerifyOtpCodeCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _jwtTokenGenerator.GenerateToken(Arg.Any<User>()).Returns("jwt-token");
    }

    // ─── Usuario no encontrado ───────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => handler.HandleAsync(BuildCommand()));
    }

    // ─── OtpCode no válido ───────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenNoValidOtpCode_ThrowsInvalidOtpCodeException()
    {
        // Usuario sin OtpCodes
        var user = User.Create("user@example.com", FixedNow);
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<InvalidOtpCodeException>(
            () => handler.HandleAsync(BuildCommand()));
    }

    [Fact]
    public async Task HandleAsync_WhenOtpCodeIsExpired_ThrowsInvalidOtpCodeException()
    {
        var user    = User.Create("user@example.com", FixedNow);
        // OtpCode creado hace 20 minutos → expirado (TTL 10 min)
        var expired = OtpCode.Create("hashed:123456", user.Id, FixedNow.AddMinutes(-20));
        user.AddOtpCode(expired);
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<InvalidOtpCodeException>(
            () => handler.HandleAsync(BuildCommand()));
    }

    [Fact]
    public async Task HandleAsync_WhenOtpCodeIsAlreadyUsed_ThrowsInvalidOtpCodeException()
    {
        var user    = User.Create("user@example.com", FixedNow);
        var otpCode = OtpCode.Create("hashed:123456", user.Id, FixedNow);
        otpCode.MarkAsUsed(FixedNow);
        user.AddOtpCode(otpCode);
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<InvalidOtpCodeException>(
            () => handler.HandleAsync(BuildCommand()));
    }

    [Fact]
    public async Task HandleAsync_WhenHashVerificationFails_ThrowsInvalidOtpCodeException()
    {
        _hashService.Verify("wrong", Arg.Any<string>()).Returns(false);
        var user    = User.Create("user@example.com", FixedNow);
        var otpCode = OtpCode.Create("hashed:123456", user.Id, FixedNow);
        user.AddOtpCode(otpCode);
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<InvalidOtpCodeException>(
            () => handler.HandleAsync(BuildCommand(code: "wrong")));
    }

    // ─── Marcado como usado ──────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ValidCode_MarksOtpCodeAsUsed()
    {
        var user = BuildUserWithValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        Assert.True(user.OtpCodes.Single().IsAlreadyUsed());
    }

    // ─── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ValidCode_ReturnsJwtToken()
    {
        var user = BuildUserWithValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        var result = await handler.HandleAsync(BuildCommand());

        Assert.Equal("jwt-token", result.AccessToken);
    }

    [Fact]
    public async Task HandleAsync_ValidCode_ReturnsNonEmptyRefreshToken()
    {
        var user = BuildUserWithValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        var result = await handler.HandleAsync(BuildCommand());

        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
    }

    [Fact]
    public async Task HandleAsync_ValidCode_RegistersDeviceWithCommandData()
    {
        var user    = BuildUserWithValidOtp();
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
    public async Task HandleAsync_ValidCode_IssuedRefreshTokenHasCorrectIp()
    {
        var user    = BuildUserWithValidOtp();
        var command = BuildCommand();
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(command);

        var refreshToken = user.DeviceTokens.Single().RefreshTokens.Single();
        Assert.Equal(command.Ip, refreshToken.CreatedByIp);
    }

    [Fact]
    public async Task HandleAsync_ValidCode_CallsJwtGeneratorWithCorrectUser()
    {
        var user = BuildUserWithValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        _jwtTokenGenerator.Received(1).GenerateToken(user);
    }

    [Fact]
    public async Task HandleAsync_ValidCode_CallsSaveChanges()
    {
        var user = BuildUserWithValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── CancellationToken ───────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_PropagatesCancellationTokenToSaveChanges()
    {
        var user = BuildUserWithValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var cts     = new CancellationTokenSource();
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(), cts.Token);

        await _uow.Received(1).SaveChangesAsync(cts.Token);
    }

    // ─── Uso del reloj ───────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_UsesClockUtcNow()
    {
        var user = BuildUserWithValidOtp();
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        _ = _clock.Received().UtcNow;
    }
}
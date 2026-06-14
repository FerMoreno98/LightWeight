using LightWeight.Auth.Application.Commands.LoginWithRefreshToken;
using LightWeight.Auth.Application.Exceptions;
using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Exceptions;
using LightWeight.Auth.Domain.Repository;
using LightWeight.Auth.Domain.Services;
using LightWeight.shared.BuildingBlocks.Persistance;
using LightWeight.shared.Types;
using NSubstitute;
using Xunit;

namespace LightWeight.Auth.Tests.Application.Commands;

public class LoginWithRefreshTokenCommandHandlerTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────────

    private readonly IUserRepository    _userRepository    = Substitute.For<IUserRepository>();
    private readonly IClock             _clock             = Substitute.For<IClock>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IUnitOfWork        _uow               = Substitute.For<IUnitOfWork>();

    private static readonly DateTime FixedNow =
        new(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private LoginWithRefreshTokenCommandHandler BuildHandler() =>
        new(_userRepository, _clock, _jwtTokenGenerator,_uow);

    public LoginWithRefreshTokenCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _jwtTokenGenerator.GenerateToken(Arg.Any<User>()).Returns("jwt-token");
    }

    /// <summary>
    /// Crea un User con un DeviceToken y un RefreshToken válido (no expirado, no revocado).
    /// </summary>
    private static (User user, string tokenValue) BuildUserWithValidToken()
    {
        var user   = User.Create("test@example.com", FixedNow);
        var device = user.RegisterDevice("device-001", "Mi Phone", "Android", FixedNow);
        var token  = device.IssueRefreshToken("192.168.1.1", FixedNow);
        return (user, token.Token);
    }

    private static LoginWithRefreshTokenCommand BuildCommand(string refreshToken, string ip = "192.168.1.1") =>
        new(refreshToken, ip);

    // ─── Usuario no encontrado ───────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        _userRepository.GetByRefreshTokenAsync(Arg.Any<string>()).Returns((User?)null);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => handler.HandleAsync(BuildCommand("token-inexistente")));
    }

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_QueriesRepositoryWithCorrectToken()
    {
        _userRepository.GetByRefreshTokenAsync("mi-token").Returns((User?)null);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => handler.HandleAsync(BuildCommand("mi-token")));

        await _userRepository.Received(1).GetByRefreshTokenAsync("mi-token");
    }

    // ─── Token no pertenece a ningún device ─────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenTokenNotFoundInAnyDevice_ThrowsDeviceNotFoundException()
    {
        var (user, _) = BuildUserWithValidToken();
        _userRepository.GetByRefreshTokenAsync("token-extrano").Returns(user);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<DeviceNotFoundException>(
            () => handler.HandleAsync(BuildCommand("token-extrano")));
    }

    // ─── Token expirado ──────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenTokenIsExpired_ThrowsInvalidRefreshTokenException()
    {
        // El token se emitió hace 31 días → expirado (TTL 30 días)
        var issuedAt = FixedNow.AddDays(-31);
        var user     = User.Create("test@example.com", issuedAt);
        var device   = user.RegisterDevice("device-001", "Mi Phone", "Android", issuedAt);
        var token    = device.IssueRefreshToken("192.168.1.1", issuedAt);
        _userRepository.GetByRefreshTokenAsync(token.Token).Returns(user);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => handler.HandleAsync(BuildCommand(token.Token)));
    }

    // ─── Token revocado manualmente (logout) ─────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenTokenRevokedWithoutReplacement_ThrowsRevokedRefreshTokenException()
    {
        var (user, tokenValue) = BuildUserWithValidToken();
        var device             = user.DeviceTokens.Single();
        var refreshToken       = device.RefreshTokens.Single();
        // Revocado por logout: RevokedAt tiene valor pero ReplacedTokenBy es null
        refreshToken.Revoke(FixedNow);
        _userRepository.GetByRefreshTokenAsync(tokenValue).Returns(user);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<RevokedRefreshTokenException>(
            () => handler.HandleAsync(BuildCommand(tokenValue)));
    }

    // ─── Token robado (revocado y ya reemplazado) ─────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenTokenRevokedAndReplaced_ThrowsStolenRefreshTokenException()
    {
        var (user, originalTokenValue) = BuildUserWithValidToken();
        var device = user.DeviceTokens.Single();
        // Rotamos el token: el original queda revocado y con ReplacedTokenBy seteado
        device.IssueRefreshToken("192.168.1.1", FixedNow);
        // Intentamos usar el token original ya rotado
        _userRepository.GetByRefreshTokenAsync(originalTokenValue).Returns(user);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<StolenRefreshTokenException>(
            () => handler.HandleAsync(BuildCommand(originalTokenValue)));
    }

    // ─── Happy path ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ValidToken_ReturnsJwtToken()
    {
        var (user, tokenValue) = BuildUserWithValidToken();
        _userRepository.GetByRefreshTokenAsync(tokenValue).Returns(user);
        var handler = BuildHandler();

        var result = await handler.HandleAsync(BuildCommand(tokenValue));

        Assert.Equal("jwt-token", result.AccessToken);
    }

    [Fact]
    public async Task HandleAsync_ValidToken_ReturnsNewNonEmptyRefreshToken()
    {
        var (user, tokenValue) = BuildUserWithValidToken();
        _userRepository.GetByRefreshTokenAsync(tokenValue).Returns(user);
        var handler = BuildHandler();

        var result = await handler.HandleAsync(BuildCommand(tokenValue));

        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
    }

    [Fact]
    public async Task HandleAsync_ValidToken_ReturnsNewDifferentRefreshToken()
    {
        var (user, tokenValue) = BuildUserWithValidToken();
        _userRepository.GetByRefreshTokenAsync(tokenValue).Returns(user);
        var handler = BuildHandler();

        var result = await handler.HandleAsync(BuildCommand(tokenValue));

        Assert.NotEqual(tokenValue, result.RefreshToken);
    }

    [Fact]
    public async Task HandleAsync_ValidToken_NewRefreshTokenHasCorrectIp()
    {
        var (user, tokenValue) = BuildUserWithValidToken();
        _userRepository.GetByRefreshTokenAsync(tokenValue).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(tokenValue, ip: "10.0.0.5"));

        var newToken = user.DeviceTokens.Single().RefreshTokens.Last();
        Assert.Equal("10.0.0.5", newToken.CreatedByIp);
    }

    [Fact]
    public async Task HandleAsync_ValidToken_AddsNewRefreshTokenToDevice()
    {
        var (user, tokenValue) = BuildUserWithValidToken();
        _userRepository.GetByRefreshTokenAsync(tokenValue).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(tokenValue));

        Assert.Equal(2, user.DeviceTokens.Single().RefreshTokens.Count);
    }

    [Fact]
    public async Task HandleAsync_ValidToken_CallsJwtGeneratorWithCorrectUser()
    {
        var (user, tokenValue) = BuildUserWithValidToken();
        _userRepository.GetByRefreshTokenAsync(tokenValue).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(tokenValue));

        _jwtTokenGenerator.Received(1).GenerateToken(user);
    }

    // ─── Uso del reloj inyectado ─────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_UsesClockUtcNow()
    {
        var (user, tokenValue) = BuildUserWithValidToken();
        _userRepository.GetByRefreshTokenAsync(tokenValue).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(tokenValue));

        _ = _clock.Received().UtcNow;
    }
}
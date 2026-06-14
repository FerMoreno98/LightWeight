using LightWeight.Auth.Application.Commands.Logout;
using LightWeight.Auth.Application.Exceptions;
using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Exceptions;
using LightWeight.Auth.Domain.Repository;
using LightWeight.shared.BuildingBlocks.Persistance;
using LightWeight.shared.Types;
using NSubstitute;
using Xunit;

namespace LightWeight.Auth.UnitTests.Application.Commands.Logout;

public class LogoutCommandHandlerTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────────

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IClock          _clock          = Substitute.For<IClock>();
    private readonly IUnitOfWork        _uow               = Substitute.For<IUnitOfWork>();

    private static readonly DateTime FixedNow =
        new(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private LogoutCommandHandler BuildHandler() =>
        new(_userRepository, _clock,_uow);

    public LogoutCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
    }

    /// <summary>
    /// Crea un User con un DeviceToken y un RefreshToken ya emitido,
    /// devuelve tanto el User como el token string para usarlo en los comandos.
    /// </summary>
    private static (User user, string refreshTokenValue) BuildUserWithDevice()
    {
        var user   = User.Create("test@example.com", DateTime.UtcNow);
        var device = user.RegisterDevice("device-001", "Mi Phone", "Android", DateTime.UtcNow);
        var token  = device.IssueRefreshToken("192.168.1.1", DateTime.UtcNow);
        return (user, token.Token);
    }

    private static LogoutCommand BuildCommand(Guid userId, string refreshToken) =>
        new(userId, refreshToken);

    // ─── Usuario no encontrado ───────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        _userRepository.FindbyIdAsync(Arg.Any<Guid>()).Returns((User?)null);
        var handler = BuildHandler();
        var command = BuildCommand(Guid.NewGuid(), "any-token");

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_QueriesRepositoryWithCorrectId()
    {
        var userId = Guid.NewGuid();
        _userRepository.FindbyIdAsync(userId).Returns((User?)null);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => handler.HandleAsync(BuildCommand(userId, "any-token")));

        await _userRepository.Received(1).FindbyIdAsync(userId);
    }

    // ─── Device / RefreshToken no encontrado ─────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenRefreshTokenNotFound_ThrowsDeviceNotFoundException()
    {
        var (user, _) = BuildUserWithDevice();
        _userRepository.FindbyIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await Assert.ThrowsAsync<DeviceNotFoundException>(
            () => handler.HandleAsync(BuildCommand(user.Id, "token-inexistente")));
    }

    // ─── Revocación correcta ─────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_RevokesCorrectRefreshToken()
    {
        var (user, tokenValue) = BuildUserWithDevice();
        _userRepository.FindbyIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(user.Id, tokenValue));

        var refreshToken = user.DeviceTokens.Single().RefreshTokens.Single();
        Assert.NotNull(refreshToken.RevokedAt);
    }

    [Fact]
    public async Task HandleAsync_SetsRevokedAtUsingClock()
    {
        var (user, tokenValue) = BuildUserWithDevice();
        _userRepository.FindbyIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(user.Id, tokenValue));

        var refreshToken = user.DeviceTokens.Single().RefreshTokens.Single();
        Assert.Equal(FixedNow, refreshToken.RevokedAt);
    }

    [Fact]
    public async Task HandleAsync_DoesNotRevokeOtherTokens()
    {
        var (user, firstTokenValue) = BuildUserWithDevice();
        // Emitimos un segundo token en el mismo device
        var secondToken = user.DeviceTokens.Single().IssueRefreshToken("10.0.0.1", DateTime.UtcNow);
        _userRepository.FindbyIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(user.Id, firstTokenValue));

        Assert.Null(secondToken.RevokedAt);
    }

    [Fact]
    public async Task HandleAsync_WithMultipleDevices_OnlyRevokesTokenFromCorrectDevice()
    {
        var user          = User.Create("test@example.com", DateTime.UtcNow);
        var deviceA       = user.RegisterDevice("device-A", "Phone",  "Android", DateTime.UtcNow);
        var deviceB       = user.RegisterDevice("device-B", "Tablet", "iOS",     DateTime.UtcNow);
        var tokenA        = deviceA.IssueRefreshToken("192.168.1.1", DateTime.UtcNow);
        var tokenB        = deviceB.IssueRefreshToken("192.168.1.2", DateTime.UtcNow);
        _userRepository.FindbyIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(user.Id, tokenA.Token));

        Assert.NotNull(tokenA.RevokedAt);
        Assert.Null(tokenB.RevokedAt);
    }

    // ─── No llama a SaveChanges ──────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_CompletesWithoutException_WhenTokenIsValid()
    {
        var (user, tokenValue) = BuildUserWithDevice();
        _userRepository.FindbyIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        var exception = await Record.ExceptionAsync(
            () => handler.HandleAsync(BuildCommand(user.Id, tokenValue)));

        Assert.Null(exception);
    }

    // ─── Uso del reloj inyectado ─────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_UsesClockUtcNow()
    {
        var (user, tokenValue) = BuildUserWithDevice();
        _userRepository.FindbyIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(user.Id, tokenValue));

        _ = _clock.Received().UtcNow;
    }

    // ─── Propagación del CancellationToken ──────────────────────────────────────

    [Fact]
    public async Task HandleAsync_PropagatesCancellationTokenToRepository()
    {
        var (user, tokenValue) = BuildUserWithDevice();
        var cts = new CancellationTokenSource();
        _userRepository.FindbyIdAsync(user.Id).Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(user.Id, tokenValue), cts.Token);

        await _userRepository.Received(1).FindbyIdAsync(user.Id);
    }
}
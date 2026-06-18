using LightWeight.Auth.Application.Commands.LoginOtp.SendOtpCode;
using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Entities;
using LightWeight.Auth.Domain.Repository;
using LightWeight.Auth.Domain.Services;
using LightWeight.Auth.Domain.Uow;
using LightWeight.shared.Types;
using NSubstitute;
using Xunit;

namespace LightWeight.Auth.Tests.Application.Commands;

public class SendOtpCodeCommandHandlerTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────────

    private readonly IAuthUnitOfWork       _uow            = Substitute.For<IAuthUnitOfWork>();
    private readonly IUserRepository   _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailSender      _emailSender    = Substitute.For<IEmailSender>();
    private readonly IClock            _clock          = Substitute.For<IClock>();
    private readonly ICodeHasher       _hasher         = Substitute.For<ICodeHasher>();

    private static readonly DateTime FixedNow =
        new(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private SendOtpCodeCommandHandler BuildHandler() =>
        new(_uow, _userRepository, _emailSender, _clock, _hasher);

    private static SendOtpCodeCommand BuildCommand(string email = "user@example.com") =>
        new(email);

    public SendOtpCodeCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _hasher.HashCode(Arg.Any<string>()).Returns(info => $"hashed:{info.Arg<string>()}");
    }

    // ─── GenerateCode ────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateCode_ReturnsExactlySixCharacters()
    {
        var code = BuildHandler().GenerateCode();
        Assert.Equal(6, code.Length);
    }

    [Fact]
    public void GenerateCode_ReturnsOnlyDigits()
    {
        var code = BuildHandler().GenerateCode();
        Assert.All(code, c => Assert.True(char.IsDigit(c)));
    }

    [Fact]
    public void GenerateCode_ValueIsInValidRange()
    {
        var code = BuildHandler().GenerateCode();
        Assert.InRange(int.Parse(code), 0, 999_999);
    }

    [Fact]
    public void GenerateCode_AlwaysZeroPadsToSixDigits()
    {
        var handler = BuildHandler();
        for (int i = 0; i < 20; i++)
            Assert.Equal(6, handler.GenerateCode().Length);
    }

    // ─── Usuario nuevo ───────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_CallsAddAsync()
    {
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        await _userRepository.Received(1)
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_AddsUserWithCorrectEmail()
    {
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        User? captured = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>());
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        Assert.NotNull(captured);
        Assert.Equal("user@example.com", captured!.Email);
    }



    // ─── Usuario existente ───────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WhenUserExists_DoesNotCallAddAsync()
    {
        var user = User.Create("user@example.com", FixedNow);
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        await _userRepository.DidNotReceive()
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task HandleAsync_WhenUserExists_AddsOtpCodeToUser()
    {
        var user = User.Create("user@example.com", FixedNow);
        _userRepository.FindByEmailAsync("user@example.com").Returns(user);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        Assert.Single(user.OtpCodes);
    }

    // ─── OtpCode ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_OtpCode_HashIsHashedPlainCode()
    {
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        User? captured = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>());
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        Assert.StartsWith("hashed:", captured!.OtpCodes.Single().Hash);
    }

    [Fact]
    public async Task HandleAsync_OtpCode_HasCorrectUserId()
    {
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        User? captured = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>());
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        Assert.Equal(captured!.Id, captured.OtpCodes.Single().UserId);
    }

    [Fact]
    public async Task HandleAsync_OtpCode_ExpiresAtTenMinutesFromNow()
    {
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        User? captured = null;
        await _userRepository.AddAsync(
            Arg.Do<User>(u => captured = u), Arg.Any<CancellationToken>());
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        Assert.Equal(FixedNow.AddMinutes(10), captured!.OtpCodes.Single().ExpiresAt);
    }

    // ─── Email ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_SendsEmailToCorrectAddress()
    {
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        await _emailSender.Received(1)
            .Send("user@example.com", Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_SendsEmailWithCorrectSubject()
    {
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        await _emailSender.Received(1)
            .Send(Arg.Any<string>(), "Verification code", Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_SendsPlainCodeNotHashInEmail()
    {
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        string? sentCode = null;
        await _emailSender.Send(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Do<string>(c => sentCode = c));
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        Assert.NotNull(sentCode);
        Assert.DoesNotContain("hashed:", sentCode);
        Assert.Equal(6, sentCode!.Length);
    }

    // ─── SaveChanges y CancellationToken ─────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_CallsSaveChanges()
    {
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand());

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PropagatesCancellationTokenToSaveChanges()
    {
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        var cts     = new CancellationTokenSource();
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(), cts.Token);

        await _uow.Received(1).SaveChangesAsync(cts.Token);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_PropagatesCancellationTokenToAddAsync()
    {
        _userRepository.FindByEmailAsync("user@example.com").Returns((User?)null);
        var cts     = new CancellationTokenSource();
        var handler = BuildHandler();

        await handler.HandleAsync(BuildCommand(), cts.Token);

        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), cts.Token);
    }
}
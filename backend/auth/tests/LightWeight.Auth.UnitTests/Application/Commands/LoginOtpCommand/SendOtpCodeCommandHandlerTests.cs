using LightWeight.Auth.Application.Commands.LoginOtp.SendOtpCode;
using LightWeight.Auth.Domain.Repository;
using LightWeight.Auth.Domain.Services;
using LightWeight.Auth.Domain.ValueObjects;
using LightWeight.shared.BuildingBlocks.Persistance;
using LightWeight.shared.Types;
using NSubstitute;
using Xunit;

namespace LightWeight.Auth.UnitTestsTests.Application.Commands.LoginOtpCommand;

public class SendOtpCodeCommandHandlerTests
{
    // ─── Fixtures ────────────────────────────────────────────────────────────────

    private readonly IUnitOfWork       _uow            = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository   _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailSender      _emailSender    = Substitute.For<IEmailSender>();
    private readonly IClock            _clock          = Substitute.For<IClock>();
    private readonly ICodeHasher       _hasher         = Substitute.For<ICodeHasher>();

    private static readonly DateTime FixedNow =
        new(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private SendOtpCodeCommandHandler BuildHandler() =>
        new(_uow, _userRepository, _emailSender, _clock, _hasher);

    public SendOtpCodeCommandHandlerTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        // El hasher devuelve un hash predecible para poder verificar qué se persiste
        _hasher.HashCode(Arg.Any<string>()).Returns(info => $"hashed:{info.Arg<string>()}");
    }

    // ─── GenerateCode ────────────────────────────────────────────────────────────

    [Fact]
    public void GenerateCode_ReturnsExactlySixCharacters()
    {
        var handler = BuildHandler();

        var code = handler.GenerateCode();

        Assert.Equal(6, code.Length);
    }

    [Fact]
    public void GenerateCode_ReturnsOnlyDigits()
    {
        var handler = BuildHandler();

        var code = handler.GenerateCode();

        Assert.All(code, c => Assert.True(char.IsDigit(c)));
    }

    [Fact]
    public void GenerateCode_ValueIsInValidRange()
    {
        var handler = BuildHandler();

        var code   = handler.GenerateCode();
        var number = int.Parse(code);

        Assert.InRange(number, 0, 999_999);
    }

    [Fact]
    public void GenerateCode_ZeroPadsToSixDigits()
    {
        // Independientemente del valor, siempre debe tener 6 caracteres (D6)
        var handler = BuildHandler();

        for (int i = 0; i < 20; i++)
        {
            var code = handler.GenerateCode();
            Assert.Equal(6, code.Length);
        }
    }

    // ─── HandleAsync: interacciones con dependencias ─────────────────────────────

    [Fact]
    public async Task HandleAsync_HashesTheGeneratedCode()
    {
        var handler = BuildHandler();
        var command = new SendOtpCodeCommand("user@example.com");

        await handler.HandleAsync(command);

        _hasher.Received(1).HashCode(Arg.Is<string>(c => c.Length == 6));
    }

    [Fact]
    public async Task HandleAsync_PersistsOtpHashViaRepository()
    {
        var handler = BuildHandler();
        var command = new SendOtpCodeCommand("user@example.com");

        await handler.HandleAsync(command);

        await _userRepository.Received(1)
            .KeepOtpCode(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PersistsTheHashReturnedByHasher()
    {
        var handler      = BuildHandler();
        var command      = new SendOtpCodeCommand("user@example.com");
        string? captured = null;

        await _userRepository
            .KeepOtpCode(Arg.Do<string>(h => captured = h), Arg.Any<CancellationToken>());

        await handler.HandleAsync(command);

        Assert.NotNull(captured);
        Assert.StartsWith("hashed:", captured);
    }

    [Fact]
    public async Task HandleAsync_SendsEmailToCommandEmail()
    {
        var handler = BuildHandler();
        var command = new SendOtpCodeCommand("user@example.com");

        await handler.HandleAsync(command);

        await _emailSender.Received(1)
            .Send("user@example.com", Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_SendsEmailWithCorrectSubject()
    {
        var handler = BuildHandler();
        var command = new SendOtpCodeCommand("user@example.com");

        await handler.HandleAsync(command);

        await _emailSender.Received(1)
            .Send(Arg.Any<string>(), "Verification code", Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_SendsPlainCodeNotHashInEmail()
    {
        var handler      = BuildHandler();
        var command      = new SendOtpCodeCommand("user@example.com");
        string? sentCode = null;

        await _emailSender
            .Send(Arg.Any<string>(), Arg.Any<string>(), Arg.Do<string>(c => sentCode = c));

        await handler.HandleAsync(command);

        Assert.NotNull(sentCode);
        Assert.DoesNotContain("hashed:", sentCode);   // el email lleva el código en plano
        Assert.Equal(6, sentCode!.Length);
    }

    [Fact]
    public async Task HandleAsync_CallsSaveChanges()
    {
        var handler = BuildHandler();
        var command = new SendOtpCodeCommand("user@example.com");

        await handler.HandleAsync(command);

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── HandleAsync: orden de operaciones ───────────────────────────────────────

    [Fact]
    public async Task HandleAsync_SaveChanges_IsCalledAfterRepositoryAndEmail()
    {
        var handler  = BuildHandler();
        var command  = new SendOtpCodeCommand("user@example.com");
        var callLog  = new List<string>();

        _userRepository
            .KeepOtpCode(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => { callLog.Add("repository"); return Task.CompletedTask; });

        _emailSender
            .Send(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(_ => { callLog.Add("email"); return Task.CompletedTask; });

        _uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => { callLog.Add("saveChanges"); return Task.CompletedTask; });

        await handler.HandleAsync(command);

        Assert.Equal(new[] { "repository", "email", "saveChanges" }, callLog);
    }

    // ─── HandleAsync: propagación del CancellationToken ─────────────────────────

    [Fact]
    public async Task HandleAsync_PropagatesCancellationTokenToRepository()
    {
        var handler = BuildHandler();
        var command = new SendOtpCodeCommand("user@example.com");
        var cts     = new CancellationTokenSource();

        await handler.HandleAsync(command, cts.Token);

        await _userRepository.Received(1)
            .KeepOtpCode(Arg.Any<string>(), cts.Token);
    }

    [Fact]
    public async Task HandleAsync_PropagatesCancellationTokenToUow()
    {
        var handler = BuildHandler();
        var command = new SendOtpCodeCommand("user@example.com");
        var cts     = new CancellationTokenSource();

        await handler.HandleAsync(command, cts.Token);

        await _uow.Received(1).SaveChangesAsync(cts.Token);
    }

    // ─── HandleAsync: usa el reloj inyectado ─────────────────────────────────────

    [Fact]
    public async Task HandleAsync_UsesClockUtcNow_ToCreateOtpCode()
    {
        var handler = BuildHandler();
        var command = new SendOtpCodeCommand("user@example.com");

        await handler.HandleAsync(command);

        // Si el clock no se consultara, el OtpCode no podría crearse con la fecha correcta
        _ = _clock.Received().UtcNow;
    }
}
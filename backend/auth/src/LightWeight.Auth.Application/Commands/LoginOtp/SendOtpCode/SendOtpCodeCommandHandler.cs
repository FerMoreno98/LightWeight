using LightWeight.shared.BuildingBlocks.Persistance;
using LightWeight.shared.Mediator;
using LightWeight.Auth.Domain.Repository;
using LightWeight.Auth.Domain.Services;
using LightWeight.Auth.Domain.ValueObjects;
using LightWeight.shared.Types;
namespace LightWeight.Auth.Application.Commands.LoginOtp.SendOtpCode;


public sealed class SendOtpCodeCommandHandler : ICommandHandler<SendOtpCodeCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IUserRepository _userRepository;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly ICodeHasher _hasher;

    public SendOtpCodeCommandHandler
    (
        IUnitOfWork uow,
        IUserRepository userRepository,
        IEmailSender emailSender,
        IClock clock,
        ICodeHasher hasher
    )
    {
        _uow = uow;
        _userRepository = userRepository;
        _emailSender = emailSender;
        _clock = clock;
        _hasher = hasher;
    }

    public async Task HandleAsync(SendOtpCodeCommand command, CancellationToken ct = default)
    {
        string code = GenerateCode();
        OtpCode otpCode = OtpCode.Create(code,_hasher,_clock.UtcNow);
        await _userRepository.KeepOtpCode(otpCode.Hash,ct);
        await _emailSender.Send(command.Email,"Verification code",code);
        await _uow.SaveChangesAsync(ct);
        

    }

    public string GenerateCode()
    {
        return Random.Shared.Next(0, 1_000_000).ToString("D6");
    }
}
using LightWeight.shared.BuildingBlocks.Persistance;
using LightWeight.shared.Mediator;
using LightWeight.Auth.Domain.Repository;
using LightWeight.Auth.Domain.Services;
using LightWeight.shared.Types;
using LightWeight.Auth.Domain.Entities;
using LightWeight.Auth.Domain.Aggregates;
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
        // Creo el codigo y lo hasheo
        string code = GenerateCode();
        string hash = _hasher.HashCode(code);
        // Veo si el usuario existe, si no existe lo creo
        User? existingUser = await _userRepository.FindByEmailAsync(command.Email);
        User user = existingUser ?? User.Create(command.Email, _clock.UtcNow);
        // Invalida los OTPs pendientes antes de crear uno nuevo
        user.InvalidatePendingOtpCodes(_clock.UtcNow);
        // Creo el objeto Otpcode y lo añado a la lista de codigos del usuario 
        OtpCode? otpCode  = OtpCode.Create(hash,user.Id,_clock.UtcNow);
        user.AddOtpCode(otpCode);
        if(existingUser is null)
        {
            // Guardo el usuario, supuestamente con el codigo que le he metido
            await _userRepository.AddAsync(user,ct); 
        } 
        
        await _uow.SaveChangesAsync(ct);
        await _emailSender.Send(command.Email,"Verification code",code);
    }

    public string GenerateCode()
    {
        return Random.Shared.Next(0, 1_000_000).ToString("D6");
    }
}
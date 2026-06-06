using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Repository;
using LightWeight.Auth.Domain.Services;
using LightWeight.Auth.Domain.ValueObjects;
using LightWeight.shared.BuildingBlocks.Persistance;
using LightWeight.shared.Mediator;
using LightWeight.shared.Types;

namespace LightWeight.Auth.Application.Commands.LoginOtp.VerifyOtpCode;

public sealed record VerifyOtpCodeCommandHandler : ICommandHandler<VerifyOtpCodeCommand, OtpLoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly ICodeHasher _hashService;
    private readonly IClock _clock;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _uow;

    public VerifyOtpCodeCommandHandler(IUserRepository userRepository, ICodeHasher hashService, IClock clock, IJwtTokenGenerator jwtTokenGenerator, IUnitOfWork uow)
    {
        _userRepository = userRepository;
        _hashService = hashService;
        _clock = clock;
        _jwtTokenGenerator = jwtTokenGenerator;
        _uow = uow;
    }

    public async Task<OtpLoginResult> HandleAsync(VerifyOtpCodeCommand command, CancellationToken ct = default)
    {
        OtpCode code = await _userRepository.GetHashedCodeAsync(command.Email);
        // Si el codigo es incorrecto lanzo una excepcion de dominio
        code.Validate(command.Code,_hashService,_clock.UtcNow);
        // Si es valido lo marco como usado
        code.MarkAsUsed();
        User? existingUser = await _userRepository.FindByEmailAsync(command.Email);
        bool isExistingUser = existingUser is not null;

        User user = existingUser ?? User.Create(command.Email, _clock.UtcNow);

        if (!isExistingUser)
            await _userRepository.AddAsync(user, ct);
        // Registro el device
        DeviceToken device = user.RegisterDevice
        (
            command.DeviceIdentifier,
            command.DeviceName, 
            command.Platform, 
            _clock.UtcNow
        );
        // Emito el token
        RefreshToken refreshToken = device.IssueRefreshToken
        (
            command.Ip, 
            _clock.UtcNow
        );
        string jwt = _jwtTokenGenerator.GenerateToken(user);
        await _uow.SaveChangesAsync(ct);

        return new OtpLoginResult(jwt, refreshToken.Token, isExistingUser);
      
    }
}

using LightWeight.Auth.Application.Exceptions;
using LightWeight.Auth.Domain.Aggregates;
using LightWeight.Auth.Domain.Entities;
using LightWeight.Auth.Domain.Repository;
using LightWeight.Auth.Domain.Services;

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

    public VerifyOtpCodeCommandHandler
    (
        IUserRepository userRepository,
        ICodeHasher hashService,
        IClock clock,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork uow
    )
    {
        _userRepository = userRepository;
        _hashService = hashService;
        _clock = clock;
        _jwtTokenGenerator = jwtTokenGenerator;
        _uow = uow;
    }

    public async Task<OtpLoginResult> HandleAsync(VerifyOtpCodeCommand command, CancellationToken ct = default)
    {
        // pillo el user, que tiene que existir si o si por que debe venir del paso previo (SendOtpCode)
        User? user = await _userRepository.FindByEmailAsync(command.Email) ?? throw new UserNotFoundException();
        // pillo el code que no este usado y que no haya expirado si no hay code, lanzo excepcion de application
        OtpCode? code = user.OtpCodes.
        SingleOrDefault(c => c.UsedAt == null && c.ExpiresAt > _clock.UtcNow) ?? throw new InvalidOtpCodeException();

        // Valido que el code que me pasan es correcto
        if (!_hashService.Verify(command.Code, code.Hash))
            throw new InvalidOtpCodeException();
        // Si es valido lo marco como usado
        code.MarkAsUsed(_clock.UtcNow);
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

        return new OtpLoginResult(jwt, refreshToken.Token);
        }

}

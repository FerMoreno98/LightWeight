using LightWeight.Auth.Domain.Exceptions;
using LightWeight.Auth.Domain.Services;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Auth.Domain.ValueObjects;

public record OtpCode : ValueObject
{
    private OtpCode(string hash, DateTime createdAt, DateTime expiresAt)
    {
        Hash = hash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public string Hash {get;}
    public DateTime CreatedAt {get;}
    public DateTime? UsedAt {get; private set;}
    public DateTime ExpiresAt {get;}

    public static OtpCode Create(string plainCode, ICodeHasher hashService,DateTime now, int minutesTtl = 10)
    {
        var hash = hashService.HashCode(plainCode);
        return new OtpCode(hash,now,now.AddMinutes(minutesTtl));
    }

    public bool IsExpired(DateTime now) => now > ExpiresAt;

    public bool IsAlreadyUsed() => UsedAt.HasValue;

    public bool Validate(string plainCode, ICodeHasher hashService,DateTime now)
        {
        if (IsExpired(now)) throw new OtpCodeExpiredException();
        if (IsAlreadyUsed()) throw new OtpCodeAlreadyUsedException();
        if (!hashService.Verify(plainCode, Hash)) throw new InvalidOtpCodeException();

        return true;
    }

    public void MarkAsUsed() => UsedAt = DateTime.UtcNow;
}
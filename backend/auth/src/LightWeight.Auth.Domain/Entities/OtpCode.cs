using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Auth.Domain.Entities;

public sealed class OtpCode : Entity<Guid>
{
    private OtpCode(Guid id,string hash, Guid userId,DateTime createdAt, DateTime expiresAt) : base (id)
    {
        Hash = hash;
        UserId = userId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public string Hash {get;}
    public Guid UserId{get; private set;}
    public DateTime CreatedAt {get;}
    public DateTime? UsedAt {get; private set;}
    public DateTime ExpiresAt {get;}

    public static OtpCode Create(string hash,Guid userId, DateTime now, int minutesTtl = 10)
    {
        
        return new OtpCode(Guid.CreateVersion7(),hash,userId,now,now.AddMinutes(minutesTtl));
    }

    public bool IsExpired(DateTime now) => now > ExpiresAt;

    public bool IsAlreadyUsed() => UsedAt.HasValue;

    // public bool Validate(string plainCode, bool IsValid,DateTime now)
    // {
    //     if (IsExpired(now)) throw new OtpCodeExpiredException();
    //     if (IsAlreadyUsed()) throw new OtpCodeAlreadyUsedException();
    //     if (!IsValid) throw new InvalidOtpCodeException();

    //     return true;
    // }

    public void MarkAsUsed(DateTime now) => UsedAt = now;
}
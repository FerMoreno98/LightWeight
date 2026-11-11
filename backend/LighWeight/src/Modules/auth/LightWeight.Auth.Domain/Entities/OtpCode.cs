

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
    public int FailedAttempts {get; private set;}

    /// <summary>Minutes a code is valid; the email tells the user</summary>
    public const int DefaultTtlMinutes = 10;

    /// <summary>
    /// Wrong guesses a code allows. A 6 digit code is one in a million: with 5 tries it cannot be guessed,
    /// and the API also limits per IP how many codes can be requested
    /// </summary>
    public const int MaxFailedAttempts = 5;

    public static OtpCode Create(string hash,Guid userId, DateTime now, int minutesTtl = DefaultTtlMinutes)
    {
        
        return new OtpCode(Guid.CreateVersion7(),hash,userId,now,now.AddMinutes(minutesTtl));
    }

    public bool IsExpired(DateTime now) => now > ExpiresAt;

    public bool IsAlreadyUsed() => UsedAt.HasValue;

    public bool HasTooManyFailedAttempts() => FailedAttempts >= MaxFailedAttempts;

    /// <summary>Not used, not expired and still with attempts left</summary>
    public bool CanBeVerified(DateTime now) => !IsAlreadyUsed() && !IsExpired(now) && !HasTooManyFailedAttempts();

    public void RegisterFailedAttempt() => FailedAttempts++;

    // public bool Validate(string plainCode, bool IsValid,DateTime now)
    // {
    //     if (IsExpired(now)) throw new OtpCodeExpiredException();
    //     if (IsAlreadyUsed()) throw new OtpCodeAlreadyUsedException();
    //     if (!IsValid) throw new InvalidOtpCodeException();

    //     return true;
    // }

    public void MarkAsUsed(DateTime now) => UsedAt = now;
}
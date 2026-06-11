using System.Collections.ObjectModel;
using LightWeight.Auth.Domain.Entities;
using LightWeight.Auth.Domain.Events;
using LightWeight.Auth.Domain.Exceptions;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Auth.Domain.Aggregates;


public sealed class User : AggregateRoot<Guid>
{
    private User
    (
    Guid id,
    string email
    ) : base(id)
    {
        Email = email;
    }

    public string Email {get; private set;}
    private List<DeviceToken> _deviceTokens  = new();
    public IReadOnlyCollection<DeviceToken> DeviceTokens => _deviceTokens.AsReadOnly();

    private List<OtpCode> _otpCodes = new();
    public IReadOnlyCollection<OtpCode> OtpCodes => _otpCodes.AsReadOnly();

    public static User Create
    (
        string email,
        DateTime now
    )
    {
        User user=new User(Guid.CreateVersion7(),email);
        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id,now));
         return user;
    }

            /// <summary>
        /// Register a device and links it to a user
        /// </summary>
        /// <param name="deviceIdentifier"></param>
        /// <param name="deviceName"></param>
        /// <param name="platform"></param>
        /// <param name="now"></param>
        /// <param name="revokedAt"></param>
        /// <returns></returns>
        public DeviceToken RegisterDevice
        (
            string deviceIdentifier,
            string deviceName,
            string platform,
            DateTime now,
            DateTime? revokedAt=null 
        )
        {
            DeviceToken? existing=_deviceTokens.FirstOrDefault(d=>d.DeviceIdentifier==deviceIdentifier);
            if (existing is not null)
            {
                existing.UpdateLastSeen(now);
                return existing;
            }
            else
            {
            DeviceToken device= new DeviceToken
            (
                Guid.CreateVersion7(),
                this.Id,
                deviceIdentifier,
                deviceName,
                platform,
                now,
                now,
                revokedAt
            );
            _deviceTokens.Add(device);
            return device;
            }
        }
    public DeviceToken GetDeviceByRefreshToken(string refreshToken)
    {
        DeviceToken? device = 
        DeviceTokens.FirstOrDefault(d=> d.RefreshTokens.Any(r=>r.Token==refreshToken)) ?? throw new DeviceNotFoundException();
        return device;
    }
    public void AddOtpCode(OtpCode code)
    {
        _otpCodes.Add(code);
    }
    public void InvalidatePendingOtpCodes(DateTime utcNow)
{
    foreach (var otp in _otpCodes.Where(o => o.UsedAt is null))
    {
        otp.MarkAsUsed(utcNow);
    }
}

}
using System.Collections.ObjectModel;
using LightWeight.Auth.Domain.Events;
using LightWeight.Auth.Domain.ValueObjects;
using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Auth.Domain.Aggregates;


public sealed class User : AggregateRoot<Guid>
{
    private User
    (
    Guid Uid,
    string email
    ) : base(Uid)
    {
        Email = email;
    }

    public string Email {get; private set;}

    public List<RefreshToken> _refreshTokens  = new();
    public ReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public List<DeviceToken> _deviceTokens  = new();
    public ReadOnlyCollection<DeviceToken> DeviceTokens => _deviceTokens.AsReadOnly();

    public List<OtpCode> _otpCodes = new();
    public ReadOnlyCollection<OtpCode> OtpCodes => _otpCodes.AsReadOnly();

    public static User Create
    (
        string email,
        DateTime now
    )
    {
        User user=new User(Guid.CreateVersion7(),email);
        user.RaiseDomainEvent(new UserCreatedDomainEvent(email,now));
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
            // RaiseDomainEvent(
            // new DeviceTokenAddedDomainEvent
            // (
            //     device.Id,
            //     this.Id,
            //     deviceIdentifier,
            //     deviceName,
            //     platform,
            //     now,
            //     now,
            //     revokedAt,
            //     now
            // )
            // );
            return device;
            }
        }

}
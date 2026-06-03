using System.Collections.ObjectModel;
using LightWeight.shared.BuildingBlocks;

namespace auth.Domain.Aggregates;


public sealed class User : AggregateRoot<Guid>
{
    public User
    (
    Guid Uid,
    string email
    ) : base(Uid)
    {
        Email = email;
    }

    public string Email {get; private set;}

    public List<RefreshToken> RefreshToken = new();
    public ReadOnlyCollection<RefreshToken> _refreshTokens => RefreshToken.AsReadOnly();

    public List<DeviceToken> DeviceToken = new();
    public ReadOnlyCollection<DeviceToken> _deviceToken => DeviceToken.AsReadOnly();

}
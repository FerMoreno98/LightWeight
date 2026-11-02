using LightWeight.shared.Exceptions;

namespace LightWeight.Auth.Domain.Exceptions;

public sealed class DeviceNotFoundException : AuthDomainException, INotFoundException
{
    public DeviceNotFoundException() : base("Device not found")
    {
        
    }
}
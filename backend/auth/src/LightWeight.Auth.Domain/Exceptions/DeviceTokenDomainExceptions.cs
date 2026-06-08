namespace LightWeight.Auth.Domain.Exceptions;

public sealed class DeviceNotFoundException : AuthDomainException
{
    public DeviceNotFoundException() : base("Device not found")
    {
        
    }
}
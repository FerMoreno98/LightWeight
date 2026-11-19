using LightWeight.shared.Exceptions;

namespace LightWeight.DeviceIntegration.Application.Exceptions;

public abstract class DeviceIntegrationApplicationException : BusinessException
{
    protected DeviceIntegrationApplicationException(string message) : base(message)
    {

    }
}

public sealed class FatSecretNotConfiguredApplicationException : DeviceIntegrationApplicationException
{
    public FatSecretNotConfiguredApplicationException() : base("The connection with FatSecret is not available")
    {

    }
}

public sealed class FatSecretConnectionNotFoundApplicationException : DeviceIntegrationApplicationException, INotFoundException
{
    public FatSecretConnectionNotFoundApplicationException() : base("There is no FatSecret account linked")
    {

    }
}

/// <summary>FatSecret failed or answered something unexpected while linking</summary>
public sealed class FatSecretUnavailableApplicationException : DeviceIntegrationApplicationException
{
    public FatSecretUnavailableApplicationException() : base("FatSecret is not answering right now. Try again later")
    {

    }
}

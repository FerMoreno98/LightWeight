using LightWeight.shared.Exceptions;

namespace LightWeight.DeviceIntegration.Domain.Exceptions;

public abstract class DeviceIntegrationDomainException : BusinessException
{
    protected DeviceIntegrationDomainException(string message) : base(message)
    {

    }
}

public sealed class UserIdEmptyDomainException : DeviceIntegrationDomainException
{
    public UserIdEmptyDomainException() : base("The connection must belong to a user")
    {

    }
}
public sealed class InvalidOAuthTokenDomainException : DeviceIntegrationDomainException
{
    public InvalidOAuthTokenDomainException() : base("FatSecret returned an empty token")
    {

    }
}
public sealed class NoPendingLinkingDomainException : DeviceIntegrationDomainException, IConflictException
{
    public NoPendingLinkingDomainException() : base("There is no FatSecret linking in progress. Start it again")
    {

    }
}
public sealed class LinkingRequestMismatchDomainException : DeviceIntegrationDomainException
{
    public LinkingRequestMismatchDomainException() : base("This FatSecret authorization does not belong to the linking in progress")
    {

    }
}
public sealed class LinkingRequestExpiredDomainException : DeviceIntegrationDomainException
{
    public LinkingRequestExpiredDomainException() : base("The FatSecret authorization took too long. Start it again")
    {

    }
}
public sealed class FatSecretNotLinkedDomainException : DeviceIntegrationDomainException, IConflictException
{
    public FatSecretNotLinkedDomainException() : base("The FatSecret account is not linked")
    {

    }
}

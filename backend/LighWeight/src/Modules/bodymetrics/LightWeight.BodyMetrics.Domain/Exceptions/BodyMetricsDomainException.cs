using LightWeight.shared.Exceptions;

namespace LightWeight.BodyMetrics.Domain.Exceptions;

public abstract class BodyMetricsDomainException : BusinessException
{
    public BodyMetricsDomainException(string message) : base(message)
    {

    }
}

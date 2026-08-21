namespace LightWeight.bodymetrics.Domain.Exceptions;

public abstract class BodyMetricsDomainException : Exception
{
    public BodyMetricsDomainException(string message) : base(message)
    {
        
    }
}
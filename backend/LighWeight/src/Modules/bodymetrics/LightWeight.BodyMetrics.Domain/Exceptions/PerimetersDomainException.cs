namespace LightWeight.bodymetrics.Domain.Exceptions;

public sealed class NegativePerimeterException : BodyMetricsDomainException
{
    public NegativePerimeterException(string perimeter):base($"{perimeter}  can't be negative")
    {
        
    }
}
public sealed class EveryPerimeterNullException : BodyMetricsDomainException
{
    public EveryPerimeterNullException() : base("It is necessary to register at least one perimeter")
    {
        
    }
}
namespace LightWeight.bodymetrics.Domain.Exceptions;

public sealed class NegativeSkinFoldException : BodyMetricsDomainException
{
    public NegativeSkinFoldException(string skinfold) : base($"{skinfold} can't be negative")
    {
        
    }
}
public sealed class EverySkinFoldNullException : BodyMetricsDomainException
{
    public EverySkinFoldNullException() : base ("It is necessary to register at least one perimeter"){

    }
}
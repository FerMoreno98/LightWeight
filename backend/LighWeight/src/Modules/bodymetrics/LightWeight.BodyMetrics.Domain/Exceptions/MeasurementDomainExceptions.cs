namespace LightWeight.BodyMetrics.Domain.Exceptions;

public sealed class UserIdEmptyDomainException : BodyMetricsDomainException
{
    public UserIdEmptyDomainException() : base("The measurement must belong to a user")
    {

    }
}
public sealed class MeasureOutOfRangeDomainException : BodyMetricsDomainException
{
    public MeasureOutOfRangeDomainException(string measure, decimal min, decimal max)
        : base($"{measure} must be between {min} and {max}")
    {

    }
}
public sealed class MeasuredInTheFutureDomainException : BodyMetricsDomainException
{
    public MeasuredInTheFutureDomainException() : base("A measurement cannot be taken in the future")
    {

    }
}
public sealed class NoPerimeterMeasuredDomainException : BodyMetricsDomainException
{
    public NoPerimeterMeasuredDomainException() : base("It is necessary to register at least one perimeter")
    {

    }
}
public sealed class NoSkinFoldMeasuredDomainException : BodyMetricsDomainException
{
    public NoSkinFoldMeasuredDomainException() : base("It is necessary to register at least one skinfold")
    {

    }
}

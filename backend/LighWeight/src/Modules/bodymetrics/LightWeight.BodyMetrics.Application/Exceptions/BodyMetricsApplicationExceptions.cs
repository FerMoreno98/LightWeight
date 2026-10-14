namespace LightWeight.BodyMetrics.Application.Exceptions;

public abstract class BodyMetricsApplicationException : Exception
{
    protected BodyMetricsApplicationException(string message) : base(message)
    {

    }
}

public sealed class MeasurementNotFoundApplicationException : BodyMetricsApplicationException
{
    public MeasurementNotFoundApplicationException() : base("Measurement not found")
    {

    }
}

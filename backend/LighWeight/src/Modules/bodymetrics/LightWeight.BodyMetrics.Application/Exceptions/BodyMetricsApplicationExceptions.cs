using LightWeight.shared.Exceptions;

namespace LightWeight.BodyMetrics.Application.Exceptions;

public abstract class BodyMetricsApplicationException : BusinessException
{
    protected BodyMetricsApplicationException(string message) : base(message)
    {

    }
}

public sealed class MeasurementNotFoundApplicationException : BodyMetricsApplicationException, INotFoundException
{
    public MeasurementNotFoundApplicationException() : base("Measurement not found")
    {

    }
}

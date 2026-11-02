namespace LightWeight.shared.Exceptions;

/// <summary>
/// Base of every expected error of the modules: a business rule was broken and the message can be shown to the client.
/// The global exception handler answers 400, or a more specific status with the markers below.
/// Any other exception is a bug and is answered as a 500 without details
/// </summary>
public abstract class BusinessException : Exception
{
    protected BusinessException(string message) : base(message)
    {
    }
}

/// <summary>Marks a <see cref="BusinessException"/> for something that does not exist (404)</summary>
public interface INotFoundException
{
}

/// <summary>Marks a <see cref="BusinessException"/> that collides with the current state: already exists or already finished (409)</summary>
public interface IConflictException
{
}

using LightWeight.shared.Exceptions;

namespace LightWeight.Training.Application.Exceptions;

public sealed class MacrocycleNotFoundException : TrainingApplicationException, INotFoundException
{
    public MacrocycleNotFoundException() : base("This macrocycle does not belong to this user or does not exists")
    {
        
    }
}
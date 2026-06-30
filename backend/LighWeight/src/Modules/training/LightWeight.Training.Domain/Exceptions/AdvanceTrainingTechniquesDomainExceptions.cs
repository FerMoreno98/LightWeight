namespace LightWeight.Training.Domain.Exceptions;

public sealed class AdvanceTrainingTechniquesExceptions : TrainingDomainException
{
    public AdvanceTrainingTechniquesExceptions() : base("There can't be 2 or more advance techniques at the same set")
    {
        
    }
}
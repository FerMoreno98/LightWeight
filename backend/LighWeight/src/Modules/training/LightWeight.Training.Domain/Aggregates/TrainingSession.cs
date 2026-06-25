using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Entities;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class TrainingSession : AggregateRoot<Guid>
{
    public DateTime? StartAt {get; private set;}
    public TimeOnly? Duration {get; private set;}
    
    private List<Exercise> _exercises = new();
    public IReadOnlyCollection<Exercise> Exercises => _exercises.AsReadOnly();

}
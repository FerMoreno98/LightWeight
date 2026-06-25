using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class Microcycle : AggregateRoot<Guid>
{
    public Guid MesocycleId{get;private set;}
    public int DurationInDays{get;private set;}
    public string TrainingDistribution{get;private set;} 
    
}
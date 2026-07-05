using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class Microcycle : AggregateRoot<Guid>
{
    private Microcycle
    (
        Guid Id,
        Guid mesocycleId, 
        int durationInDays, 
        TrainingDistribution trainingDistribution
    ) : base(Id)
    {
        MesocycleId = mesocycleId;
        DurationInDays = durationInDays;
        TrainingDistribution = trainingDistribution;
    }

    /// <summary>Parent mesocycle ID</summary>
    public Guid MesocycleId{get;private set;}
    /// <summary>Number of days this microcycle spans (typically 7)</summary>
    public int DurationInDays{get;private set;}
    /// <summary>Distribution type (Push/Pull/Legs, Upper/Lower, etc.)</summary>
    public TrainingDistribution TrainingDistribution{get;private set;} 

    /// <summary>Creates a new microcycle within a mesocycle</summary>
    /// <param name="mesocycleId">Parent mesocycle ID</param>
    /// <param name="durationInDays">Duration in days</param>
    /// <param name="trainingDistribution">Weekly distribution pattern</param>
    public static Microcycle Create
    (
        Guid mesocycleId, 
        int durationInDays, 
        TrainingDistribution trainingDistribution
    )
    {
        return new Microcycle(Guid.CreateVersion7(),mesocycleId,durationInDays,trainingDistribution);
    }
    
}
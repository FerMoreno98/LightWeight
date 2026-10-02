using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class Microcycle : AggregateRoot<Guid>
{
    private Microcycle
    (
        Guid Id,
        Guid mesocycleId, 
        Guid userId,
        Guid trainingTemplateId,
        int weekNumber
    ) : base(Id)
    {
        MesocycleId = mesocycleId;
        UserId = userId;
        TrainingTemplateId = trainingTemplateId;
        WeekNumber = weekNumber;
    }

    /// <summary>Parent mesocycle ID</summary>
    public Guid MesocycleId{get;private set;}
    public Guid UserId{get; private set;}

    public Guid TrainingTemplateId{get;private set;}
    public int WeekNumber{get; private set;}

    /// <summary>Creates a new microcycle within a mesocycle</summary>
    /// <param name="mesocycleId">Parent mesocycle ID</param>
    /// <param name="durationInDays">Duration in days</param>
    /// <param name="trainingDistribution">Weekly distribution pattern</param>
    public static Microcycle Create
    (
        Guid mesocycleId,
        Guid userId,
        Guid TrainingTemplateId,
        int WeekNumber
    )
    {
        return new Microcycle(Guid.CreateVersion7(),mesocycleId,userId,TrainingTemplateId,WeekNumber);
    }
    
}
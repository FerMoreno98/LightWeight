using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Training.Domain.Aggregates;

/// <summary>A week of a mesocycle, following one of the templates of the mesocycle's program</summary>
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

    /// <summary>Template followed this week</summary>
    public Guid TrainingTemplateId{get;private set;}
    /// <summary>Position of the week inside the mesocycle (1, 2, 3...). Assigned by the Mesocycle</summary>
    public int WeekNumber{get; private set;}

    /// <summary>Creates a new microcycle. Only the Mesocycle creates them (see Mesocycle.PlanMicrocycle)</summary>
    internal static Microcycle Create
    (
        Guid mesocycleId,
        Guid userId,
        Guid trainingTemplateId,
        int weekNumber
    )
    {
        return new Microcycle(Guid.CreateVersion7(),mesocycleId,userId,trainingTemplateId,weekNumber);
    }
}

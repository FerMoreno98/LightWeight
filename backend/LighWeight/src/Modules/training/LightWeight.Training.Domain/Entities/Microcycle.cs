using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Exceptions;

namespace LightWeight.Training.Domain.Entities;

/// <summary>
/// A week of a mesocycle, following one of the templates of the mesocycle's program.
/// Weeks are sequential: a mesocycle has at most one active week at a time
/// </summary>
public sealed class Microcycle : Entity<Guid>
{
    private Microcycle
    (
        Guid Id,
        Guid mesocycleId,
        Guid userId,
        Guid trainingTemplateId,
        int weekNumber,
        DateTime startedAt
    ) : base(Id)
    {
        MesocycleId = mesocycleId;
        UserId = userId;
        TrainingTemplateId = trainingTemplateId;
        WeekNumber = weekNumber;
        StartedAt = startedAt;
    }

    /// <summary>Parent mesocycle ID</summary>
    public Guid MesocycleId{get;private set;}
    public Guid UserId{get; private set;}

    /// <summary>Template followed this week</summary>
    public Guid TrainingTemplateId{get;private set;}
    /// <summary>Position of the week inside the mesocycle (1, 2, 3...). Assigned by the Mesocycle</summary>
    public int WeekNumber{get; private set;}
    /// <summary>Date the week was started (set by the system on creation)</summary>
    public DateTime StartedAt{get; private set;}
    /// <summary>Date the week was finished (null while active)</summary>
    public DateTime? FinishedAt{get; private set;}
    /// <summary>Whether the week has been finished</summary>
    public bool IsFinished => FinishedAt is not null;

    /// <summary>Creates a new microcycle. Only the Mesocycle creates them (see Mesocycle.PlanMicrocycle)</summary>
    internal static Microcycle Create
    (
        Guid mesocycleId,
        Guid userId,
        Guid trainingTemplateId,
        int weekNumber,
        DateTime now
    )
    {
        return new Microcycle(Guid.CreateVersion7(),mesocycleId,userId,trainingTemplateId,weekNumber,now);
    }

    /// <summary>Finishes the week, so the next one can be planned</summary>
    /// <param name="now">Completion timestamp</param>
    internal void Finish(DateTime now)
    {
        if(IsFinished)
            throw new MicrocycleFinishedDomainException();
        FinishedAt = now;
    }
}

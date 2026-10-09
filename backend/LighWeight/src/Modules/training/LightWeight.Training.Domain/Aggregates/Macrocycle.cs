using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;

namespace LightWeight.Training.Domain.Aggregates;

/// <summary>
/// Long training block (months). Cycles are sequential: a user has at most one active macrocycle,
/// and a macrocycle has at most one active mesocycle at a time
/// </summary>
public sealed class Macrocycle : AggregateRoot<Guid>
{

    private Macrocycle
    (
        Guid Id,
        Guid userId,
        DateTime startedAt,
        TrainingStage stage,
        string? comments
    ) : base(Id)
    {
        UserId = userId;
        StartedAt = startedAt;
        Stage = stage;
        Comments = comments;
    }
    /// <summary>Owner of the macrocycle</summary>
    public Guid UserId {get; private set;}

    /// <summary>Date the macrocycle was started (set by the system on creation)</summary>
    public DateTime StartedAt{get; private set;}
    /// <summary>Date the macrocycle was finished (null while active)</summary>
    public DateTime? FinishedAt{get; private set;}
    /// <summary>Whether the macrocycle has been finished</summary>
    public bool IsFinished => FinishedAt is not null;
    /// <summary>Training stage (bulk, cut, maintenance)</summary>
    public TrainingStage Stage{get;private set;}

    /// <summary>Optional notes about the macrocycle</summary>
    public string? Comments{get;private set;}

    /// <summary>Starts a new macrocycle for a user</summary>
    /// <param name="userId">Owner ID</param>
    /// <param name="stage">Training stage</param>
    /// <param name="comments">Optional notes</param>
    /// <param name="now">Start timestamp</param>
    /// <param name="activeMacrocycle">The user's current active macrocycle, if any. It must be finished first</param>
    public static Macrocycle Create
    (
        Guid userId,
        TrainingStage stage,
        string? comments,
        DateTime now,
        Macrocycle? activeMacrocycle
    )
    {
        if(userId == Guid.Empty)
            throw new UserIdEmptyDomainException();
        if(activeMacrocycle is not null && !activeMacrocycle.IsFinished)
            throw new ActiveMacrocycleAlreadyExistsDomainException();
        return new Macrocycle(Guid.CreateVersion7(),userId,now,stage,comments);
    }

    /// <summary>Starts a new mesocycle in this macrocycle, following the given program</summary>
    /// <param name="mesocycles">Mesocycles already in this macrocycle. All of them must be finished</param>
    /// <param name="programId">Program the mesocycle follows</param>
    /// <param name="motivationLevel">Motivation level at the start (1-10)</param>
    /// <param name="injuries">Injuries to track during the block</param>
    /// <param name="comments">Optional notes</param>
    /// <param name="now">Start timestamp</param>
    public Mesocycle PlanMesocycle
    (
        IReadOnlyCollection<Mesocycle> mesocycles,
        Guid programId,
        int motivationLevel,
        string? injuries,
        string? comments,
        DateTime now
    )
    {
        if(IsFinished)
            throw new MacrocycleFinishedDomainException();
        if(mesocycles.Any(m => !m.IsFinished))
            throw new ActiveMesocycleAlreadyExistsDomainException();
        return Mesocycle.Create(Id, UserId, programId, motivationLevel, injuries, comments, now);
    }

    /// <summary>Finishes the macrocycle and, with the same timestamp, its active mesocycle (if any)</summary>
    /// <param name="now">Completion timestamp</param>
    /// <param name="mesocycles">Mesocycles of this macrocycle</param>
    public void Finish(DateTime now, IReadOnlyCollection<Mesocycle> mesocycles)
    {
        if(IsFinished)
            throw new MacrocycleFinishedDomainException();
        foreach(var mesocycle in mesocycles.Where(m => !m.IsFinished))
            mesocycle.Finish(now);
        FinishedAt = now;
    }
}

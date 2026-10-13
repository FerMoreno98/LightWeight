using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;

namespace LightWeight.Training.Domain.Aggregates;

/// <summary>
/// Long training block (months). Aggregate root of the cycle hierarchy: Macrocycle -> Mesocycles -> Microcycles.
/// Cycles are sequential: a user has at most one active macrocycle, a macrocycle at most one active mesocycle
/// and a mesocycle at most one active week
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

    private List<Mesocycle> _mesocycles = new();
    /// <summary>Mesocycles of this macrocycle</summary>
    public IReadOnlyCollection<Mesocycle> Mesocycles => _mesocycles.AsReadOnly();
    /// <summary>The mesocycle in progress (null when every mesocycle is finished or there is none)</summary>
    public Mesocycle? ActiveMesocycle => _mesocycles.SingleOrDefault(m => !m.IsFinished);
    /// <summary>The week in progress of the active mesocycle (null if there is none)</summary>
    public Microcycle? ActiveMicrocycle => ActiveMesocycle?.ActiveMicrocycle;

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
    /// <param name="programId">Program the mesocycle follows</param>
    /// <param name="motivationLevel">Motivation level at the start (1-10)</param>
    /// <param name="injuries">Injuries to track during the block</param>
    /// <param name="comments">Optional notes</param>
    /// <param name="now">Start timestamp</param>
    /// <returns>The new mesocycle, already added to the macrocycle</returns>
    public Mesocycle PlanMesocycle
    (
        Guid programId,
        int motivationLevel,
        string? injuries,
        string? comments,
        DateTime now
    )
    {
        if(IsFinished)
            throw new MacrocycleFinishedDomainException();
        if(ActiveMesocycle is not null)
            throw new ActiveMesocycleAlreadyExistsDomainException();
        Mesocycle mesocycle = Mesocycle.Create(Id, UserId, programId, motivationLevel, injuries, comments, now);
        _mesocycles.Add(mesocycle);
        return mesocycle;
    }

    /// <summary>Adds the next week to one of the mesocycles of this macrocycle</summary>
    /// <param name="mesocycleId">Mesocycle that gets the week. It must be active and have no active week</param>
    /// <param name="trainingTemplateId">Template of the mesocycle's program to follow this week</param>
    /// <param name="now">Start timestamp</param>
    /// <returns>The new microcycle, numbered after the last week</returns>
    public Microcycle PlanMicrocycle(Guid mesocycleId, Guid trainingTemplateId, DateTime now)
    {
        return GetMesocycle(mesocycleId).PlanMicrocycle(trainingTemplateId, now);
    }

    /// <summary>Finishes one of the mesocycles of this macrocycle and its active week</summary>
    /// <param name="mesocycleId">Mesocycle to finish</param>
    /// <param name="now">Completion timestamp</param>
    public void FinishMesocycle(Guid mesocycleId, DateTime now)
    {
        GetMesocycle(mesocycleId).Finish(now);
    }

    /// <summary>Finishes one of the weeks of this macrocycle, so the next one can be planned</summary>
    /// <param name="microcycleId">Week to finish</param>
    /// <param name="now">Completion timestamp</param>
    public void FinishMicrocycle(Guid microcycleId, DateTime now)
    {
        Mesocycle mesocycle = _mesocycles.SingleOrDefault(m => m.Microcycles.Any(w => w.Id == microcycleId))
            ?? throw new MicrocycleNotFoundDomainException();
        mesocycle.FinishMicrocycle(microcycleId, now);
    }

    /// <summary>Finishes the macrocycle and, with the same timestamp, its active mesocycle and active week (if any)</summary>
    /// <param name="now">Completion timestamp</param>
    public void Finish(DateTime now)
    {
        if(IsFinished)
            throw new MacrocycleFinishedDomainException();
        ActiveMesocycle?.Finish(now);
        FinishedAt = now;
    }

    private Mesocycle GetMesocycle(Guid mesocycleId)
    {
        return _mesocycles.SingleOrDefault(m => m.Id == mesocycleId)
            ?? throw new MesocycleNotFoundDomainException();
    }
}

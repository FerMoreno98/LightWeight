using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Exceptions;

namespace LightWeight.Training.Domain.Entities;

/// <summary>
/// Training block inside a macrocycle that follows a Program. Its microcycles use the templates of that program.
/// It is part of the Macrocycle aggregate: it is only created and changed through the Macrocycle
/// </summary>
public sealed class Mesocycle : Entity<Guid>
{
    /// <summary>Parent macrocycle ID</summary>
    public Guid MacrocycleId {get; private set;}
    public Guid UserId{get;private set;}
    /// <summary>User's motivation level at the start (1-10)</summary>
    public int MotivationLevel{get;private set;}
    /// <summary>Injuries the user wants to track during this block</summary>
    public string? Injuries {get;private set;}
    /// <summary>Optional notes</summary>
    public string? Comments {get;private set;}
    /// <summary>Date the mesocycle was started (set by the system on creation)</summary>
    public DateTime StartedAt{get;private set;}
    /// <summary>Date the mesocycle was finished (null while active)</summary>
    public DateTime? FinishedAt{get; private set;}
    /// <summary>Whether the mesocycle has been finished</summary>
    public bool IsFinished => FinishedAt is not null;
    /// <summary>Program whose templates are used by the microcycles</summary>
    public Guid ProgramId{get; private set;}
    private List<Microcycle> _microcycles = new();
    /// <summary>Weeks of this mesocycle</summary>
    public IReadOnlyCollection<Microcycle> Microcycles => _microcycles.AsReadOnly();
    /// <summary>The week in progress (null when every week is finished or there is none)</summary>
    public Microcycle? ActiveMicrocycle => _microcycles.SingleOrDefault(m => !m.IsFinished);

    private Mesocycle
    (
        Guid Id,
        Guid macrocycleId,
        Guid userId,
        Guid programId,
        int motivationLevel,
        string? injuries,
        string? comments,
        DateTime startedAt
    ) : base(Id)
    {
        MacrocycleId = macrocycleId;
        UserId = userId;
        ProgramId = programId;
        MotivationLevel = motivationLevel;
        Injuries = injuries;
        Comments = comments;
        StartedAt = startedAt;
    }

    /// <summary>Creates a new mesocycle. Only the Macrocycle creates them (see Macrocycle.PlanMesocycle)</summary>
    internal static Mesocycle Create
    (
        Guid macrocycleId,
        Guid userId,
        Guid programId,
        int motivationLevel,
        string? injuries,
        string? comments,
        DateTime startedAt
    )
    {
        return new Mesocycle
        (
            Guid.CreateVersion7(),
            macrocycleId,
            userId,
            programId,
            motivationLevel,
            injuries,
            comments,
            startedAt
        );
    }

    /// <summary>Finishes the mesocycle and, with the same timestamp, its active week (if any)</summary>
    /// <param name="now">Completion timestamp</param>
    internal void Finish(DateTime now)
    {
        if(IsFinished)
            throw new MesocycleFinishedDomainException();
        ActiveMicrocycle?.Finish(now);
        FinishedAt = now;
    }

    /// <summary>Adds the next week to this mesocycle using one of the templates of its program</summary>
    /// <param name="trainingTemplateId">Template of the mesocycle's program to follow this week</param>
    /// <param name="now">Start timestamp</param>
    /// <returns>The new microcycle, numbered after the last week</returns>
    internal Microcycle PlanMicrocycle(Guid trainingTemplateId, DateTime now)
    {
        if(IsFinished)
            throw new MesocycleFinishedDomainException();
        if(ActiveMicrocycle is not null)
            throw new ActiveMicrocycleAlreadyExistsDomainException();
        int nextWeek = _microcycles.Count == 0 ? 1 : _microcycles.Max(m => m.WeekNumber) + 1;
        Microcycle microcycle = Microcycle.Create(Id, UserId, trainingTemplateId, nextWeek, now);
        _microcycles.Add(microcycle);
        return microcycle;
    }

    /// <summary>Finishes one of the weeks of this mesocycle</summary>
    /// <param name="microcycleId">Week to finish</param>
    /// <param name="now">Completion timestamp</param>
    internal void FinishMicrocycle(Guid microcycleId, DateTime now)
    {
        Microcycle microcycle = _microcycles.SingleOrDefault(m => m.Id == microcycleId)
            ?? throw new MicrocycleNotFoundDomainException();
        microcycle.Finish(now);
    }
}

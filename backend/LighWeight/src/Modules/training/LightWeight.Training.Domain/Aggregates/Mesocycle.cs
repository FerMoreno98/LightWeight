using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Exceptions;

namespace LightWeight.Training.Domain.Aggregates;

/// <summary>
/// Training block inside a macrocycle that follows a Program. Its microcycles use the templates of that program
/// </summary>
public sealed class Mesocycle : AggregateRoot<Guid>
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

    /// <summary>Finishes the mesocycle, so a new one can be started in the macrocycle</summary>
    /// <param name="now">Completion timestamp</param>
    public void Finish(DateTime now)
    {
        if(IsFinished)
            throw new MesocycleFinishedDomainException();
        FinishedAt = now;
    }

    /// <summary>Adds the next week to this mesocycle using one of the templates of its program</summary>
    /// <param name="microcycles">Microcycles already in this mesocycle</param>
    /// <param name="trainingTemplateId">Template of the mesocycle's program to follow this week</param>
    /// <returns>The new microcycle, numbered after the last week</returns>
    public Microcycle PlanMicrocycle(IReadOnlyCollection<Microcycle> microcycles, Guid trainingTemplateId)
    {
        if(IsFinished)
            throw new MesocycleFinishedDomainException();
        int nextWeek = microcycles.Count == 0 ? 1 : microcycles.Max(m => m.WeekNumber) + 1;
        return Microcycle.Create(Id, UserId, trainingTemplateId, nextWeek);
    }
}

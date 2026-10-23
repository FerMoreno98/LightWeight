using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Events;
using LightWeight.Training.Domain.Exceptions;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Domain.Aggregates;

/// <summary>
/// A session actually performed by the user during a week, usually following a session of the week's template.
/// Its sets record what was really done, so they can follow the plan, replace its exercise or be extra sets
/// </summary>
public sealed class TrainingSession : AggregateRoot<Guid>
{
    /// <summary>Parent microcycle ID</summary>
    public Guid MicrocycleId{get;private set;}
    public Guid UserId{get;private set;}
    /// <summary>Name of the session (e.g. "Push A", "Upper")</summary>
    public string Name{get; private set;}
    /// <summary>Date and time the session started</summary>
    public DateTime StartAt {get; private set;}
    /// <summary>Date and time the session was finished (null while in progress)</summary>
    public DateTime? FinishedAt {get; private set;}
    /// <summary>Whether the session has been finished. A finished session is read only</summary>
    public bool IsFinished => FinishedAt is not null;
    /// <summary>Total duration of the session (zero while in progress)</summary>
    public TimeSpan Duration {get; private set;}
    /// <summary>Optional notes about the session</summary>
    public string? Comments{get;private set;}
    /// <summary>Motivation level recorded at the start (1-10)</summary>
    public int MotivationLevel{get; private set;}
    /// <summary>Sleep quality before the session (1-10)</summary>
    public int SleepLevel{get; private set;}
    /// <summary>Delayed onset muscle soreness level (1-10)</summary>
    public int DOMSLevel{get; private set;}
    /// <summary>Session of the template this session follows</summary>
    public Guid? TemplateSessionId{get; private set;}

    private List<Set> _sets = new();

    private TrainingSession
    (
        Guid Id,
        Guid microcycleId,
        Guid userId,
        string name,
        DateTime startAt,
        TimeSpan duration,
        string? comments,
        int motivationLevel,
        int sleepLevel,
        int dOMSLevel,
        Guid? templateSessionId
    ) : base(Id)
    {
        MicrocycleId = microcycleId;
        UserId = userId;
        Name = name;
        StartAt = startAt;
        Duration = duration;
        Comments = comments;
        MotivationLevel = motivationLevel;
        SleepLevel = sleepLevel;
        DOMSLevel = dOMSLevel;
        TemplateSessionId = templateSessionId;
    }

    /// <summary>Sets registered in this session, in the order they were performed</summary>
    public IReadOnlyCollection<Set> Sets => _sets.AsReadOnly();

    /// <summary>Starts a new training session</summary>
    /// <param name="microcycleId">Week the session belongs to</param>
    /// <param name="userId">Owner ID</param>
    /// <param name="name">Session name</param>
    /// <param name="now">Start timestamp</param>
    /// <param name="comments">Optional notes</param>
    /// <param name="motivationLevel">Motivation (1-10)</param>
    /// <param name="sleepLevel">Sleep quality (1-10)</param>
    /// <param name="dOMSLevel">DOMS level (1-10)</param>
    /// <param name="TemplateSessionId">Session of the template it follows</param>
    public static TrainingSession Create
    (
        Guid microcycleId,
        Guid userId,
        string name,
        DateTime now,
        string? comments,
        int motivationLevel,
        int sleepLevel,
        int dOMSLevel,
        Guid? TemplateSessionId = null
    )
    {
        if(userId == Guid.Empty)
            throw new UserIdEmptyDomainException();
        if(string.IsNullOrWhiteSpace(name))
            throw new NameEmptyDomainException();
        return new TrainingSession
        (
            Guid.CreateVersion7(),
            microcycleId,
            userId,
            name,
            now,
            TimeSpan.Zero,
            comments,
            motivationLevel,
            sleepLevel,
            dOMSLevel,
            TemplateSessionId
        );
    }

    /// <summary>Finishes the session and calculates its total duration</summary>
    /// <param name="now">Completion timestamp</param>
    public void Finish(DateTime now)
    {
        EnsureInProgress();
        FinishedAt = now;
        Duration = now - StartAt;
        RaiseDomainEvent(new TrainingSessionFinishedDomainEvent(Id, UserId, MicrocycleId, now));
    }

    /// <summary>
    /// Checks the session can be discarded (deleted with its sets). Only a session in progress can be discarded:
    /// a finished one is part of the history and may have completed its week
    /// </summary>
    public void EnsureCanBeDiscarded()
    {
        EnsureInProgress();
    }

    /// <summary>Registers a performed set in this session</summary>
    /// <param name="exerciseId">Exercise actually performed (it may replace the planned one)</param>
    /// <param name="templateSetId">Planned set it covers, null for an extra set</param>
    /// <param name="advanceTrainingTechniques">Technique applied (copied from the planned set)</param>
    /// <param name="superSetGroupId">Superset it was performed in: the planned one, one improvised in the session or none</param>
    /// <param name="repetitions">Reps completed</param>
    /// <param name="weight">Load in kg</param>
    /// <param name="rpe">RPE (1-10)</param>
    /// <param name="isBodyWeight">Whether it was bodyweight only</param>
    /// <returns>The new set</returns>
    public Set RegisterSet
    (
        Guid exerciseId,
        Guid? templateSetId,
        AdvanceTrainingTechniques advanceTrainingTechniques,
        Guid? superSetGroupId,
        int repetitions,
        decimal weight,
        decimal rpe,
        bool isBodyWeight
    )
    {
        EnsureInProgress();
        // A planned set is covered once: to change it the registered set is updated
        if(templateSetId is not null && _sets.Any(s => s.TemplateSetId == templateSetId))
            throw new TemplateSetAlreadyRegisteredDomainException();
        Set set = Set.Create
        (
            exerciseId,
            repetitions,
            isBodyWeight,
            advanceTrainingTechniques,
            weight,
            rpe,
            superSetGroupId,
            templateSetId
        );
        _sets.Add(set);
        return set;
    }

    /// <summary>Corrects a registered set, including the exercise performed</summary>
    public void UpdateSet(Guid setId, Guid exerciseId, int repetitions, decimal weight, decimal rpe, bool isBodyWeight)
    {
        EnsureInProgress();
        GetSet(setId).Update(exerciseId, repetitions, weight, rpe, isBodyWeight);
    }

    /// <summary>
    /// Joins registered sets in a superset or separates them (null). Supersets of real sets are decided while training,
    /// so they are not required to have two exercises yet: the next sets may be registered later
    /// </summary>
    /// <param name="setIds">Registered sets of this session</param>
    /// <param name="superSetGroupId">Superset to put them in, null to separate them</param>
    public void AssignSuperSet(IReadOnlyCollection<Guid> setIds, Guid? superSetGroupId)
    {
        EnsureInProgress();
        List<Set> sets = setIds.Distinct().Select(GetSet).ToList();
        foreach (var set in sets)
            set.SetSuperSet(superSetGroupId);
    }

    /// <summary>Removes a registered set</summary>
    public void RemoveSet(Guid setId)
    {
        EnsureInProgress();
        _sets.Remove(GetSet(setId));
    }

    private Set GetSet(Guid setId)
    {
        return _sets.SingleOrDefault(s => s.Id == setId)
            ?? throw new PerformedSetNotFoundDomainException();
    }

    private void EnsureInProgress()
    {
        if(IsFinished)
            throw new TrainingSessionFinishedDomainException();
    }
}

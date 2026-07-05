using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class TrainingSession : AggregateRoot<Guid>
{
    /// <summary>Parent microcycle ID</summary>
    public Guid MicrocycleId{get;private set;}
    /// <summary>Name of the session (e.g. "Push A", "Upper")</summary>
    public string Name{get; private set;}
    /// <summary>Date and time the session started</summary>
    public DateTime StartAt {get; private set;}
    /// <summary>Total duration of the session</summary>
    public TimeSpan Duration {get; private set;}
    /// <summary>Optional notes about the session</summary>
    public string? Comments{get;private set;}
    /// <summary>Motivation level recorded at the start (1-10)</summary>
    public int MotivationLevel{get; private set;}
    /// <summary>Sleep quality before the session (1-10)</summary>
    public int SleepLevel{get; private set;}
    /// <summary>Delayed onset muscle soreness level (1-10)</summary>
    public int DOMSLevel{get; private set;}
    
    private List<Set> _sets = new();

    private TrainingSession
    (
        Guid Id,
        Guid microcycleId,
        string name, 
        DateTime startAt, 
        TimeSpan duration, 
        string? comments, 
        int motivationLevel, 
        int sleepLevel, 
        int dOMSLevel
    ) : base(Id)
    {
        MicrocycleId = microcycleId;
        Name = name;
        StartAt = startAt;
        Duration = duration;
        Comments = comments;
        MotivationLevel = motivationLevel;
        SleepLevel = sleepLevel;
        DOMSLevel = dOMSLevel;
    }

    /// <summary>Sets registered in this session</summary>
    public IReadOnlyCollection<Set> Sets => _sets.AsReadOnly();
    /// <summary>Creates a new training session</summary>
    /// <param name="microcycleId">Parent microcycle ID</param>
    /// <param name="name">Session name</param>
    /// <param name="now">Current timestamp for start time</param>
    /// <param name="comments">Optional notes</param>
    /// <param name="motivationLevel">Motivation (1-10)</param>
    /// <param name="sleepLevel">Sleep quality (1-10)</param>
    /// <param name="dOMSLevel">DOMS level (1-10)</param>
    public static TrainingSession Create
    (
        Guid microcycleId,
        string name, 
        DateTime now,
        string? comments, 
        int motivationLevel, 
        int sleepLevel, 
        int dOMSLevel      
    )
    {
        return new TrainingSession
        (
            Guid.CreateVersion7(),
            microcycleId,
            name,
            now,
            TimeSpan.FromTicks(0),
            comments,
            motivationLevel,
            sleepLevel,
            dOMSLevel
        );
    }
    /// <summary>Ends the session and calculates total duration</summary>
    /// <param name="now">Current timestamp</param>
    public void EndTraining(DateTime now) => Duration = TimeOnly.FromDateTime(now) - TimeOnly.FromDateTime(StartAt) ;
    /// <summary>Registers a performed set in this session</summary>
    /// <param name="set">The set to add</param>
    public void RegisterSet(Set set)
    {
        _sets.Add(set);
    }

}
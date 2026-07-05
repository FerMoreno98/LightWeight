using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class Mesocycle : AggregateRoot<Guid>
{
    /// <summary>Parent macrocycle ID</summary>
    public Guid MacrocycleId {get; private set;}
    /// <summary>User's motivation level at the start (1-10)</summary>
    public int MotivationLevel{get;private set;}
    /// <summary>Injuries the user wants to track during this block</summary>
    public string? Injuries {get;private set;}
    /// <summary>Optional notes</summary>
    public string? Comments {get;private set;}
    /// <summary>Date the mesocycle starts</summary>
    public DateTime StartAt{get;private set;}
    /// <summary>Date the mesocycle ends</summary>
    public DateTime EndAt{get; private set;}

    private Mesocycle
    (
        Guid Id,
        Guid macrocycleId,
        int motivationLevel, 
        string? injuries, 
        string? comments, 
        DateTime startAt, 
        DateTime endAt
    ) : base(Id)
    {
        MacrocycleId = macrocycleId;
        MotivationLevel = motivationLevel;
        Injuries = injuries;
        Comments = comments;
        StartAt = startAt;
        EndAt = endAt;
    }
    /// <summary>Creates a new mesocycle within a macrocycle</summary>
    /// <param name="macrocycleId">Parent macrocycle ID</param>
    /// <param name="motivationLevel">Motivation level (1-10)</param>
    /// <param name="injuries">Any injuries to track</param>
    /// <param name="comments">Optional notes</param>
    /// <param name="startAt">Start date</param>
    /// <param name="endAt">End date</param>
    public static Mesocycle Create
    (
        Guid macrocycleId,
        int motivationLevel,
        string? injuries,
        string? comments,
        DateTime startAt,
        DateTime endAt
    )
    {
        return new Mesocycle
        (
            Guid.CreateVersion7(),
            macrocycleId,
            motivationLevel,
            injuries,
            comments,
            startAt,
            endAt
        );
    }
}




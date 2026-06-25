using LightWeight.shared.BuildingBlocks;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class Mesocycle : AggregateRoot<Guid>
{
    public Guid MacrocycleId {get; private set;}
    public int MotivationLevel{get;private set;}
    public string? Injuries {get;private set;}
    public string? Comments {get;private set;}

    public DateTime StartAt{get;private set;}
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


}
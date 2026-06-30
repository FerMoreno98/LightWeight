using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class Macrocycle : AggregateRoot<Guid>
{

    private Macrocycle
    (
        Guid Id,
        Guid userId,
        List<MuscleGroups> aimMuscleGroups, 
        DateTime startAt, 
        DateTime? endAt,
        TrainingStage stage,
        Periodization periodization,
        string? comments
    ) : base(Id)
    {
        UserId = userId;
        _aimMuscleGroups = aimMuscleGroups;
        StartAt = startAt;
        EndAt = endAt;
        Stage = stage;
        Periodization = periodization;
        Comments = comments;
    }
    /// <summary>Owner of the macrocycle</summary>
    public Guid UserId {get; private set;}
    private List<MuscleGroups> _aimMuscleGroups = new();
    /// <summary>Muscle groups the user aims to develop during this macrocycle</summary>
    public IReadOnlyCollection<MuscleGroups> AimMuscleGroups => _aimMuscleGroups.AsReadOnly();
    /// <summary>Date the macrocycle starts</summary>
    public DateTime StartAt{get; private set;}
    /// <summary>Date the macrocycle ends (null while active)</summary>
    public DateTime? EndAt{get; private set;}
    /// <summary>Training stage (bulk, cut, maintenance)</summary>
    public TrainingStage Stage{get;private set;}
    /// <summary>Type of periodization used</summary>
    public Periodization Periodization{get;private set;}
    /// <summary>Optional notes about the macrocycle</summary>
    public string? Comments{get;private set;}

    /// <summary>Creates a new macrocycle for a user</summary>
    /// <param name="UserId">Owner ID</param>
    /// <param name="AimMuscles">Target muscle groups</param>
    /// <param name="StartedAt">Start date</param>
    /// <param name="EndAt">Expected end date</param>
    /// <param name="stage">Training stage</param>
    /// <param name="periodization">Periodization type</param>
    /// <param name="comments">Optional notes</param>
    public static Macrocycle Create
    (
        Guid UserId,
        List<MuscleGroups> AimMuscles, 
        DateTime StartedAt, 
        DateTime? EndAt,
        TrainingStage stage,
        Periodization periodization,
        string? comments
    )
    {
        return new Macrocycle(Guid.CreateVersion7(),UserId,AimMuscles,StartedAt,EndAt,stage,periodization,comments);
    }

    /// <summary>Marks the macrocycle as finished at the given time</summary>
    /// <param name="now">Completion timestamp</param>
    public void Finish(DateTime now) => EndAt = now;


}
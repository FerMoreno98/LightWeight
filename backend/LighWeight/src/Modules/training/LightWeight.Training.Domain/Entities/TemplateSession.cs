using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;

namespace LightWeight.Training.Domain.Entities;

public sealed class TemplateSession : Entity<Guid>
{
    /// <summary>Name of the template session (e.g. "Push A", "Upper")</summary>
    public string Name{get; private set;}
    private List<TemplateSet> _templateExercises = new();
    /// <summary>Soft delete flag: deleted sessions are kept so performed sessions can still reference them</summary>
    public bool IsDeleted { get; private set; }
    /// <summary>Date the session was soft deleted (null while active)</summary>
    public DateTime? DeletedAt { get; private set; }

    private TemplateSession
    (
        Guid Id,
        string name
    ) : base(Id)
    {
        Name = name;
    }

    /// <summary>Planned sets for this session (soft deleted sets are excluded)</summary>
    public IReadOnlyCollection<TemplateSet> TemplateExercises => _templateExercises.Where(s => !s.IsDeleted).ToList().AsReadOnly();

    /// <summary>Creates a new template session</summary>
    /// <param name="name">Session name</param>
    public static TemplateSession Create
    (
        string name
    )
    {
        if(name.Trim() == "")
            throw new SessionNameEmptyDomainException();
        return new TemplateSession
        (
            Guid.CreateVersion7(),
            name
        );
    }

    public void AddSet(TemplateSet set)
    {
        _templateExercises.Add(set);
    }

    public Dictionary<MuscleGroups,int> GetNumberOfSeriesPerGroupPerSession()
    {
        var NumberOfSeries = new Dictionary<MuscleGroups,int>();
        foreach(var sets in TemplateExercises)
        {
            foreach(var musclegroup in sets.AimMuscleGroups)
            {
                NumberOfSeries[musclegroup] = NumberOfSeries.GetValueOrDefault(musclegroup) + 1;
            }
        }
        return NumberOfSeries;

    }
    /// <summary>Soft deletes a planned set of this session</summary>
    /// <param name="TemplateSetId">Set to delete</param>
    /// <param name="now">Deletion timestamp</param>
    public void DeleteTemplateSet(Guid TemplateSetId, DateTime now)
    {
       TemplateSet? set = _templateExercises.SingleOrDefault(e => e.Id == TemplateSetId && !e.IsDeleted)
       ?? throw new SetNotFoundDomainException();
       set.MarkAsDeleted(now);
    }

    /// <summary>Marks the session and all its sets as deleted without removing them from the database</summary>
    /// <param name="now">Deletion timestamp</param>
    internal void MarkAsDeleted(DateTime now)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = now;
        foreach (var set in _templateExercises)
            set.MarkAsDeleted(now);
    }
}
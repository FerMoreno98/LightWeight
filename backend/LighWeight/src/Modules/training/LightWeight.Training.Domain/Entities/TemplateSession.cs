using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;

namespace LightWeight.Training.Domain.Entities;

public sealed class TemplateSession : Entity<Guid>
{
    /// <summary>Name of the template session (e.g. "Push A", "Upper")</summary>
    public string Name{get; private set;}
    private List<TemplateSet> _templateExercises = new();

    private TemplateSession
    (
        Guid Id,
        string name
    ) : base(Id)
    {
        Name = name;
    }

    /// <summary>Planned sets for this session</summary>
    public IReadOnlyCollection<TemplateSet> TemplateExercises => _templateExercises.AsReadOnly();

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
        foreach(var sets in _templateExercises)
        {
            foreach(var musclegroup in sets.AimMuscleGroups)
            {
                NumberOfSeries[musclegroup] = NumberOfSeries.GetValueOrDefault(musclegroup) + 1;
            }
        }
        return NumberOfSeries;

    }
}
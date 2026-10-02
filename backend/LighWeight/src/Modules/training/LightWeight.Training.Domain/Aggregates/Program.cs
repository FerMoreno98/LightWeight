using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class Program : AggregateRoot<Guid>
{
    public Guid UserId{get; private set;}
    public string Name{get; private set;}
    /// <summary>Type of periodization used</summary>
    public Periodization Periodization{get;private set;}
    private List<TrainingTemplate> _trainingTemplates = new();
    /// <summary>Templates of this program (soft deleted templates are excluded)</summary>
    public IReadOnlyCollection<TrainingTemplate> trainingTemplates => _trainingTemplates.Where(t => !t.IsDeleted).ToList().AsReadOnly();
    public List<MuscleGroups> AimMuscleGroups {get; private set;}

    private Program
    (
        Guid Id,
        Guid userId,
        string name,
        Periodization periodization,
        List<MuscleGroups> aimMuscleGroups

    ) : base(Id)
    {
        UserId = userId;
        Name = name;
        Periodization = periodization;
        AimMuscleGroups = aimMuscleGroups;
    }
    public static Program Create
    (
        Guid UserId,
        Periodization periodization,
        string name,
        List<MuscleGroups> muscleGroups
    )
    {
        if(UserId == Guid.Empty)
            throw new UserIdEmptyDomainException();
        if(string.IsNullOrWhiteSpace(name))
            throw new NameEmptyDomainException();
        return new Program(Guid.CreateVersion7(),UserId,name,periodization,muscleGroups);
    }

    public void AddTrainingTemplate(TrainingTemplate trainingTemplate)
    {
        _trainingTemplates.Add(trainingTemplate);
    }

    /// <summary>Soft deletes a template of this program and its whole hierarchy</summary>
    /// <param name="trainingTemplateId">Template to delete</param>
    /// <param name="now">Deletion timestamp</param>
    public void DeleteTrainingTemplate(Guid trainingTemplateId, DateTime now)
    {
        TrainingTemplate? trainingTemplate = _trainingTemplates.SingleOrDefault(t => t.Id == trainingTemplateId && !t.IsDeleted)
        ?? throw new TrainingTemplateNotFoundDomainException();
        trainingTemplate.MarkAsDeleted(now);
    }
}
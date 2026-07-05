using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class TrainingTemplate : AggregateRoot<Guid>
{
    /// <summary>Owner of the template</summary>
    public Guid UserId {get; private set;}
    /// <summary>Name of the template (e.g. "Push/Pull/Legs", "Upper/Lower")</summary>
    public string Name{get; private set;}
    /// <summary>Weekly distribution pattern this template follows</summary>
    public TrainingDistribution TrainingDistribution{get;private set;} 
    private List<TemplateSession> _templateSessions = new();

    private TrainingTemplate
    (
        Guid Id,
        Guid userId, 
        string name, 
        TrainingDistribution trainingDistribution
    ) : base(Id)
    {
        UserId = userId;
        Name = name;
        TrainingDistribution = trainingDistribution;
    }

    /// <summary>Sessions defined in this template</summary>
    public IReadOnlyCollection<TemplateSession> TemplateSessions => _templateSessions.AsReadOnly();

    /// <summary>Creates a new training template</summary>
    /// <param name="userId">Owner ID</param>
    /// <param name="name">Template name</param>
    /// <param name="trainingDistribution">Weekly distribution pattern</param>
    public static TrainingTemplate Create
    (
        Guid userId,
        string name,
        TrainingDistribution trainingDistribution
    )
    {
        return new TrainingTemplate(Guid.CreateVersion7(),userId,name,trainingDistribution);
    }
    
}
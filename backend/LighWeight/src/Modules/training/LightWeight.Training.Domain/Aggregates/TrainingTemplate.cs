using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Domain.Aggregates;

public sealed class TrainingTemplate : AggregateRoot<Guid>
{
    public Guid UserId {get; private set;}
    public string Name{get; private set;}
    public TrainingDistribution TrainingDistribution{get;private set;} 
    private List<TemplateSession> _templateSessions = new();

    public TrainingTemplate
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

    public IReadOnlyCollection<TemplateSession> TemplateSessions => _templateSessions.AsReadOnly();
    
}
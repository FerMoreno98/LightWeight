using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;

namespace LightWeight.Training.Domain.Entities;

public sealed class TrainingTemplate : Entity<Guid>
{
    /// <summary>Weekly distribution pattern this template follows</summary>
    public TrainingDistribution TrainingDistribution{get;private set;} 
    /// <summary>
    /// Volume landmarks defined by Mike Israetel
    /// </summary>
    public VolumeLandmarks VolumeLandmark{get;private set;}
    private List<TemplateSession> _templateSessions = new();
    public int DurationInDays{get; private set;}
    public int Order{get; private set;}
    /// <summary>Soft delete flag: deleted templates are kept so microcycles can still reference them</summary>
    public bool IsDeleted { get; private set; }
    /// <summary>Date the template was soft deleted (null while active)</summary>
    public DateTime? DeletedAt { get; private set; }


    private TrainingTemplate
    (
        Guid Id,
        VolumeLandmarks volumeLandmark,
        TrainingDistribution trainingDistribution,
        int durationInDays,
        int order
    ) : base(Id)
    {
        VolumeLandmark = volumeLandmark;
        TrainingDistribution = trainingDistribution;
        DurationInDays = durationInDays;
        Order = order;
    }

    /// <summary>Sessions defined in this template (soft deleted sessions are excluded)</summary>
    public IReadOnlyCollection<TemplateSession> TemplateSessions => _templateSessions.Where(s => !s.IsDeleted).ToList().AsReadOnly();

    /// <summary>Creates a new training template</summary>
    /// <param name="userId">Owner ID</param>
    /// <param name="trainingDistribution">Weekly distribution pattern</param>
    public static TrainingTemplate Create
    (
        VolumeLandmarks volumeLandmark,
        TrainingDistribution trainingDistribution,
        int durationInDays,
        int order
    )
    {
        if(!System.Enum.IsDefined(volumeLandmark))
        {
            throw new InvalidVolumeLandmarkDomainException();
        }
        if(!System.Enum.IsDefined(trainingDistribution))
        {
            throw new InvalidTrainingDistributionDomainException();
        }

        return new TrainingTemplate(Guid.CreateVersion7(),volumeLandmark,trainingDistribution, durationInDays,order);
    }
    
    public void AddSessionTemplate(TemplateSession session)
    {
        _templateSessions.Add(session);
    }

    public Dictionary<MuscleGroups,int> GetNumberOfSeriesPerGroup()
    {
        var ret = new Dictionary<MuscleGroups,int>();
        foreach(var session in TemplateSessions)
        {
            var SeriesPerMusclePerSession = session.GetNumberOfSeriesPerGroupPerSession();
            foreach(var muscle in SeriesPerMusclePerSession.Keys)
            {
                if (!ret.ContainsKey(muscle))
                {
                    ret[muscle] = SeriesPerMusclePerSession[muscle];
                }else
                {
                    ret[muscle] += SeriesPerMusclePerSession[muscle];
                }

            }
        }
        return ret;
    }

    /// <summary>Soft deletes a session of this template and all its sets</summary>
    /// <param name="TemplateSessionId">Session to delete</param>
    /// <param name="now">Deletion timestamp</param>
    public void DeleteSessionTemplate(Guid TemplateSessionId, DateTime now)
    {
        TemplateSession? session = _templateSessions.SingleOrDefault(ts => ts.Id == TemplateSessionId && !ts.IsDeleted)
        ?? throw new SessionNotFoundDomainException();
        session.MarkAsDeleted(now);
    }

    /// <summary>Marks the template and its whole hierarchy as deleted without removing it from the database</summary>
    /// <param name="now">Deletion timestamp</param>
    internal void MarkAsDeleted(DateTime now)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = now;
        foreach (var session in _templateSessions)
            session.MarkAsDeleted(now);
    }
}
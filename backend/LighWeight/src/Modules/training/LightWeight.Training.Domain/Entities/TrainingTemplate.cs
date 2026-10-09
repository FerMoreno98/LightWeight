using LightWeight.shared.BuildingBlocks;
using LightWeight.Training.Domain.Enum;
using LightWeight.Training.Domain.Exceptions;

namespace LightWeight.Training.Domain.Entities;

public sealed class TrainingTemplate : Entity<Guid>
{
    /// <summary>Max length of the name (matches the database column)</summary>
    public const int NameMaxLength = 200;
    /// <summary>Suffix added to the name of a duplicated template</summary>
    public const string CopySuffix = " (copia)";

    /// <summary>Name chosen by the user to tell templates apart (e.g. "Hipertrofia base", "Deload"). It does not need to be unique</summary>
    public string Name { get; private set; }
    /// <summary>Weekly distribution pattern this template follows</summary>
    public TrainingDistribution TrainingDistribution{get;private set;} 
    /// <summary>
    /// Volume landmarks defined by Mike Israetel
    /// </summary>
    public VolumeLandmarks VolumeLandmark{get;private set;}
    private List<TemplateSession> _templateSessions = new();
    public int DurationInDays{get; private set;}
    /// <summary>Position inside the program, only used for sorting. Assigned by the Program when the template is added</summary>
    public int Order{get; private set;}
    /// <summary>Soft delete flag: deleted templates are kept so microcycles can still reference them</summary>
    public bool IsDeleted { get; private set; }
    /// <summary>Date the template was soft deleted (null while active)</summary>
    public DateTime? DeletedAt { get; private set; }


    private TrainingTemplate
    (
        Guid Id,
        string name,
        VolumeLandmarks volumeLandmark,
        TrainingDistribution trainingDistribution,
        int durationInDays
    ) : base(Id)
    {
        Name = name;
        VolumeLandmark = volumeLandmark;
        TrainingDistribution = trainingDistribution;
        DurationInDays = durationInDays;
    }

    /// <summary>Sessions defined in this template (soft deleted sessions are excluded)</summary>
    public IReadOnlyCollection<TemplateSession> TemplateSessions => _templateSessions.Where(s => !s.IsDeleted).ToList().AsReadOnly();

    /// <summary>Creates a new training template. Its order is assigned when it is added to a Program</summary>
    /// <param name="name">Name of the template</param>
    /// <param name="volumeLandmark">Volume landmark the template targets</param>
    /// <param name="trainingDistribution">Weekly distribution pattern</param>
    /// <param name="durationInDays">Duration of the template in days</param>
    public static TrainingTemplate Create
    (
        string name,
        VolumeLandmarks volumeLandmark,
        TrainingDistribution trainingDistribution,
        int durationInDays
    )
    {
        if(string.IsNullOrWhiteSpace(name))
        {
            throw new NameEmptyDomainException();
        }
        if(!System.Enum.IsDefined(volumeLandmark))
        {
            throw new InvalidVolumeLandmarkDomainException();
        }
        if(!System.Enum.IsDefined(trainingDistribution))
        {
            throw new InvalidTrainingDistributionDomainException();
        }

        return new TrainingTemplate(Guid.CreateVersion7(),name.Trim(),volumeLandmark,trainingDistribution, durationInDays);
    }

    /// <summary>Changes the name of the template</summary>
    /// <param name="name">New name</param>
    public void Rename(string name)
    {
        if(string.IsNullOrWhiteSpace(name))
        {
            throw new NameEmptyDomainException();
        }
        Name = name.Trim();
    }

    /// <summary>Sets the position of the template inside its program. Only the Program decides it</summary>
    internal void SetOrder(int order)
    {
        Order = order;
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

    /// <summary>
    /// Creates a deep copy of this template with new ids, named "{Name} (copia)".
    /// Soft deleted sessions are not copied. Its order is assigned when it is added to the Program
    /// </summary>
    internal TrainingTemplate Duplicate()
    {
        var copy = Create(CopyName(), VolumeLandmark, TrainingDistribution, DurationInDays);
        foreach (var session in TemplateSessions)
            copy.AddSessionTemplate(session.Duplicate());
        return copy;
    }

    /// <summary>Name of the copy; the original name is shortened if needed so the suffix always fits</summary>
    private string CopyName()
    {
        int maxBaseLength = NameMaxLength - CopySuffix.Length;
        string baseName = Name.Length > maxBaseLength ? Name[..maxBaseLength].TrimEnd() : Name;
        return baseName + CopySuffix;
    }
}
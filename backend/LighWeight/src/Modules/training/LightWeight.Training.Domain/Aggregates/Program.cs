using System.Buffers.Text;
using System.Security.Cryptography;
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

    /// <summary>Length of a share token (9 random bytes in Base64Url, 72 bits)</summary>
    public const int ShareTokenLength = 12;
    /// <summary>
    /// Token of the share link (null while the program is not shared). Only the author can generate or revoke it:
    /// it is not consumed by copying and it does not expire
    /// </summary>
    public string? ShareToken { get; private set; }
    /// <summary>Program this one was copied from (null if it was created by the user). Not a FK: the original can be deleted</summary>
    public Guid? CopiedFromProgramId { get; private set; }
    /// <summary>Author of the program this one was copied from. Its name is read from the user names projection</summary>
    public Guid? CopiedFromUserId { get; private set; }

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

    /// <summary>Adds a template at the end of the program</summary>
    /// <param name="trainingTemplate">Template to add</param>
    public void AddTrainingTemplate(TrainingTemplate trainingTemplate)
    {
        // The order is only used for sorting, so the template always goes after the active ones
        int nextOrder = trainingTemplates.Count == 0 ? 1 : trainingTemplates.Max(t => t.Order) + 1;
        trainingTemplate.SetOrder(nextOrder);
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

    /// <summary>
    /// Duplicates an active template of this program with its whole hierarchy (sessions and sets).
    /// The copy gets new ids, is named "{name} (copia)" and is placed at the end of the program
    /// </summary>
    /// <param name="trainingTemplateId">Template to duplicate</param>
    /// <returns>The new template, already added to the program</returns>
    public TrainingTemplate DuplicateTrainingTemplate(Guid trainingTemplateId)
    {
        TrainingTemplate original = trainingTemplates.SingleOrDefault(t => t.Id == trainingTemplateId)
            ?? throw new TrainingTemplateNotFoundDomainException();
        TrainingTemplate copy = original.Duplicate(original.CopyName());
        AddTrainingTemplate(copy);
        return copy;
    }

    /// <summary>Renames an active template of this program</summary>
    /// <param name="trainingTemplateId">Template to rename</param>
    /// <param name="name">New name</param>
    public void RenameTrainingTemplate(Guid trainingTemplateId, string name)
    {
        TrainingTemplate trainingTemplate = trainingTemplates.SingleOrDefault(t => t.Id == trainingTemplateId)
            ?? throw new TrainingTemplateNotFoundDomainException();
        trainingTemplate.Rename(name);
    }

    /// <summary>Shares the program: generates its share token, or returns the current one if it is already shared</summary>
    /// <returns>The share token</returns>
    public string Share()
    {
        ShareToken ??= Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(9));
        return ShareToken;
    }

    /// <summary>Stops sharing the program: the current link stops working. Copies already made are not affected</summary>
    public void RevokeShare()
    {
        ShareToken = null;
    }

    /// <summary>
    /// Copies the planning of this program (templates, sessions and sets) into a new program of another user.
    /// It is a snapshot: names are kept, everything gets new ids (supersets too), soft deleted items are skipped,
    /// the templates keep their relative order and the share token is not copied.
    /// The copy remembers where it came from (program and author)
    /// </summary>
    /// <param name="userId">User that receives the copy</param>
    /// <returns>The new program, not persisted yet</returns>
    public Program CopyFor(Guid userId)
    {
        Program copy = Create(userId, Periodization, Name, AimMuscleGroups.ToList());
        copy.CopiedFromProgramId = Id;
        copy.CopiedFromUserId = UserId;
        foreach (TrainingTemplate template in trainingTemplates.OrderBy(t => t.Order))
            copy.AddTrainingTemplate(template.Duplicate(template.Name));
        return copy;
    }
}

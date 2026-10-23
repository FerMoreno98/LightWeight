using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Domain.Repositories;

/// <summary>
/// Repository of the Program aggregate. TrainingTemplate, TemplateSession and TemplateSet
/// are inner entities, so they are always loaded through their Program
/// </summary>
public interface IProgramRepository
{
    Task AddAsync(Program program, CancellationToken cancellationToken);
    Task<Program?> GetByIdAsync(Guid ProgramId);
    Task<List<Program>> GetAllProgramsOfAUserAsync(Guid UserId);
    Task<Program?> GetByTrainingTemplateIdAsync(Guid TrainingTemplateId);
    Task<Program?> GetByTemplateSessionIdAsync(Guid TemplateSessionId);
    Task<Program?> GetByTemplateSetIdAsync(Guid TemplateSetId);
    /// <summary>
    /// Data of the given planned sets, <b>including the ones deleted afterwards</b>: a finished session keeps
    /// referring to the planned sets it followed even if the template is edited later
    /// </summary>
    Task<List<PlannedSetData>> GetPlannedSetsIncludingDeletedAsync(IReadOnlyCollection<Guid> TemplateSetIds);
}

/// <summary>What a planned set aimed at: its repetition range and its muscle groups</summary>
public sealed record PlannedSetData(Guid TemplateSetId, int MinReps, int MaxReps, IReadOnlyList<MuscleGroups> AimMuscleGroups);

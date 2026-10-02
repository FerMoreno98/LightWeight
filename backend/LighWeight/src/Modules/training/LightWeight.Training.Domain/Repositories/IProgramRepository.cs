using LightWeight.Training.Domain.Aggregates;

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
}

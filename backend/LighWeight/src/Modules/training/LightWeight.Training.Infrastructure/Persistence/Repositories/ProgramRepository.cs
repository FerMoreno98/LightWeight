using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Training.Infrastructure.Persistence.Repositories;

/// <summary>
/// Always loads the whole aggregate (templates, sessions and sets).
/// Soft deleted rows are excluded by the global query filters
/// </summary>
public class ProgramRepository : IProgramRepository
{
    private readonly TrainingDbContext _dbContext;

    public ProgramRepository(TrainingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IQueryable<Program> ProgramsWithTemplates =>
        _dbContext.Programs
            .Include(p => p.trainingTemplates)
                .ThenInclude(t => t.TemplateSessions)
                    .ThenInclude(ts => ts.TemplateExercises)
            .AsSplitQuery();

    public async Task AddAsync(Program program, CancellationToken cancellationToken)
    {
        await _dbContext.AddAsync(program, cancellationToken);
    }

    public async Task<Program?> GetByIdAsync(Guid ProgramId)
    {
        return await ProgramsWithTemplates
            .SingleOrDefaultAsync(p => p.Id == ProgramId);
    }

    public async Task<List<Program>> GetAllProgramsOfAUserAsync(Guid UserId)
    {
        return await ProgramsWithTemplates
            .Where(p => p.UserId == UserId)
            .ToListAsync();
    }

    public async Task<Program?> GetByTrainingTemplateIdAsync(Guid TrainingTemplateId)
    {
        return await ProgramsWithTemplates
            .SingleOrDefaultAsync(p => p.trainingTemplates.Any(t => t.Id == TrainingTemplateId));
    }

    public async Task<Program?> GetByTemplateSessionIdAsync(Guid TemplateSessionId)
    {
        return await ProgramsWithTemplates
            .SingleOrDefaultAsync(p => p.trainingTemplates
                .Any(t => t.TemplateSessions.Any(ts => ts.Id == TemplateSessionId)));
    }

    public async Task<Program?> GetByTemplateSetIdAsync(Guid TemplateSetId)
    {
        return await ProgramsWithTemplates
            .SingleOrDefaultAsync(p => p.trainingTemplates
                .Any(t => t.TemplateSessions
                    .Any(ts => ts.TemplateExercises.Any(te => te.Id == TemplateSetId))));
    }
}

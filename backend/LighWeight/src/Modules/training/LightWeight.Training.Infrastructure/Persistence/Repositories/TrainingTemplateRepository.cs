using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Training.Infrastructure.Persistence.Repositories;

public class TrainingTemplateRepository : ITrainingTemplateRepository
{
    private readonly TrainingDbContext _dbContext;

    public TrainingTemplateRepository(TrainingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TrainingTemplate trainingTemplate, CancellationToken cancellationToken)
    {
        await _dbContext.AddAsync(trainingTemplate);
    }

    public async Task<TrainingTemplate?> GetByIdAsync(Guid TrainingTemplateId)
    {
        return await _dbContext.TrainingTemplates
            .Include(t => t.TemplateSessions)
                .ThenInclude(ts => ts.TemplateExercises)
            .SingleOrDefaultAsync(t => t.Id == TrainingTemplateId);
    }

    public async Task<TrainingTemplate?> GetBySessionIdAsync(Guid SessionId)
    {
        return await _dbContext.TrainingTemplates
            .Include(t => t.TemplateSessions)
            .SingleOrDefaultAsync(t => t.TemplateSessions.Any(ts => ts.Id == SessionId));
    }
}
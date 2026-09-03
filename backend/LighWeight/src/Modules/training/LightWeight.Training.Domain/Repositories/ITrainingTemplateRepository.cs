using LightWeight.Training.Domain.Aggregates;

namespace LightWeight.Training.Domain.Repositories;

public interface ITrainingTemplateRepository
{
    Task AddAsync(TrainingTemplate trainingTemplate, CancellationToken cancellationToken);
    Task<TrainingTemplate?> GetByIdAsync(Guid TrainingTemplateId);
    Task<TrainingTemplate?> GetBySessionIdAsync(Guid SessionId);
}
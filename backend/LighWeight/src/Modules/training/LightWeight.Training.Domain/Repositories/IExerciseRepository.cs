using LightWeight.Training.Domain.Aggregates;

namespace LightWeight.Training.Domain.Repositories;

public interface IExerciseRepository
{
    Task<List<Exercise>> GetAllAsync();
    Task<Exercise?> GetByIdAsync(Guid ExerciseId);
    Task<List<Exercise>> GetByIdsAsync(IEnumerable<Guid> ExerciseIds);
}
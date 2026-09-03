using LightWeight.Training.Domain.Aggregates;

namespace LightWeight.Training.Domain.Repositories;

public interface IExerciseRepository
{
    Task<List<Exercise>> GetAllAsync();
}
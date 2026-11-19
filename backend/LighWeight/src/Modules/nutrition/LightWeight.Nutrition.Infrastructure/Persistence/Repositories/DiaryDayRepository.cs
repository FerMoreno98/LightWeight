using LightWeight.Nutrition.Domain.Aggregates;
using LightWeight.Nutrition.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Nutrition.Infrastructure.Persistence.Repositories;

public class DiaryDayRepository : IDiaryDayRepository
{
    private readonly NutritionDbContext _dbContext;

    public DiaryDayRepository(NutritionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DiaryDay day, CancellationToken cancellationToken)
    {
        await _dbContext.DiaryDays.AddAsync(day, cancellationToken);
    }

    public async Task<List<DiaryDay>> GetBetweenAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        return await _dbContext.DiaryDays
            .Where(d => d.UserId == userId && d.Date >= from && d.Date <= to)
            .OrderBy(d => d.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DiaryDay>> GetAllOfAUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.DiaryDays
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderBy(d => d.Date)
            .ToListAsync(cancellationToken);
    }

    public void Remove(DiaryDay day)
    {
        _dbContext.DiaryDays.Remove(day);
    }
}

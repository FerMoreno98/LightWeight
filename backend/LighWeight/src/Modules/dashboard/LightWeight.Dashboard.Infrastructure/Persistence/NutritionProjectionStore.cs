using LightWeight.Dashboard.Application.Projections.Nutrition;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Dashboard.Infrastructure.Persistence;

public sealed class NutritionProjectionStore : INutritionProjectionStore
{
    private readonly DashboardDbContext _context;

    public NutritionProjectionStore(DashboardDbContext context)
        => _context = context;

    // Find by id: overwrite every value if it exists, add it otherwise
    public async Task UpsertDayAsync(NutritionDayProjection projection, CancellationToken ct = default)
    {
        NutritionDayProjection? existing = await _context.NutritionDays.FindAsync([projection.DiaryDayId], ct);
        if (existing is null)
            _context.NutritionDays.Add(projection);
        else
            _context.Entry(existing).CurrentValues.SetValues(projection);

        await _context.SaveChangesAsync(ct);
    }

    public Task DeleteDayAsync(Guid diaryDayId, Guid userId, CancellationToken ct = default)
        => _context.NutritionDays.Where(d => d.DiaryDayId == diaryDayId && d.UserId == userId).ExecuteDeleteAsync(ct);

    public Task ClearUserAsync(Guid userId, CancellationToken ct = default)
        => _context.NutritionDays.Where(d => d.UserId == userId).ExecuteDeleteAsync(ct);

    public Task<List<NutritionDayProjection>> GetDaysAsync(Guid userId, CancellationToken ct = default)
        => _context.NutritionDays.AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderBy(d => d.Date)
            .ToListAsync(ct);
}

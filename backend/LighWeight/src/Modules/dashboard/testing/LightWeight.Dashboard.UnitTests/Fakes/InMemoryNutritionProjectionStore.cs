using LightWeight.Dashboard.Application.Projections.Nutrition;

namespace LightWeight.Dashboard.UnitTests.Fakes;

/// <summary>Store with the same semantics as the real one (upsert by DiaryDayId, deletes filtered by user), kept in memory</summary>
internal sealed class InMemoryNutritionProjectionStore : INutritionProjectionStore
{
    public Dictionary<Guid, NutritionDayProjection> Days { get; } = [];

    /// <summary>Test helper: a day of the user with these totals</summary>
    public NutritionDayProjection Add(Guid userId, DateOnly date, decimal calories, decimal protein = 150m, decimal carbohydrate = 250m, decimal fat = 70m)
    {
        var day = new NutritionDayProjection
        {
            DiaryDayId = Guid.CreateVersion7(), UserId = userId, Date = date,
            Calories = calories, Protein = protein, Carbohydrate = carbohydrate, Fat = fat
        };
        Days[day.DiaryDayId] = day;
        return day;
    }

    public Task UpsertDayAsync(NutritionDayProjection projection, CancellationToken ct = default)
    {
        Days[projection.DiaryDayId] = projection;
        return Task.CompletedTask;
    }

    public Task DeleteDayAsync(Guid diaryDayId, Guid userId, CancellationToken ct = default)
    {
        if (Days.TryGetValue(diaryDayId, out var day) && day.UserId == userId)
            Days.Remove(diaryDayId);
        return Task.CompletedTask;
    }

    public Task ClearUserAsync(Guid userId, CancellationToken ct = default)
    {
        foreach (Guid id in Days.Values.Where(d => d.UserId == userId).Select(d => d.DiaryDayId).ToList())
            Days.Remove(id);
        return Task.CompletedTask;
    }

    public Task<List<NutritionDayProjection>> GetDaysAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(Days.Values.Where(d => d.UserId == userId).OrderBy(d => d.Date).ToList());
}

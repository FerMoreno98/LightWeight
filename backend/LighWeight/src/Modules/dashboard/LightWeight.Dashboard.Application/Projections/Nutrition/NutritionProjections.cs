namespace LightWeight.Dashboard.Application.Projections.Nutrition;

/// <summary>Daily totals of the food diary of a user (from Nutrition, which reads them from FatSecret)</summary>
public sealed class NutritionDayProjection
{
    /// <summary>Id of the day in Nutrition: the key of the upsert</summary>
    public Guid DiaryDayId { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Calendar day of the user (Europe/Madrid)</summary>
    public DateOnly Date { get; set; }
    /// <summary>kcal</summary>
    public decimal Calories { get; set; }
    /// <summary>Grams</summary>
    public decimal Protein { get; set; }
    public decimal Carbohydrate { get; set; }
    public decimal Fat { get; set; }
}

/// <summary>Persistence of the diary days. Each operation saves at once</summary>
public interface INutritionProjectionStore
{
    Task UpsertDayAsync(NutritionDayProjection projection, CancellationToken ct = default);
    /// <summary>Only the row of that user: an event can never remove another user's day</summary>
    Task DeleteDayAsync(Guid diaryDayId, Guid userId, CancellationToken ct = default);
    Task ClearUserAsync(Guid userId, CancellationToken ct = default);
    /// <summary>Every day of the user, from the oldest</summary>
    Task<List<NutritionDayProjection>> GetDaysAsync(Guid userId, CancellationToken ct = default);
}

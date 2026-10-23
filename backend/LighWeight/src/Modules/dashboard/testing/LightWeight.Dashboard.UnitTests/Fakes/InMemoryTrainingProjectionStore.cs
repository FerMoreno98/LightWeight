using LightWeight.Dashboard.Application.Projections.Training;

namespace LightWeight.Dashboard.UnitTests.Fakes;

/// <summary>Store with the same semantics as the real one (upsert of the session, its sets replaced), kept in memory</summary>
internal sealed class InMemoryTrainingProjectionStore : ITrainingProjectionStore
{
    public Dictionary<Guid, TrainingSessionProjection> Sessions { get; } = [];
    public Dictionary<Guid, TrainingSetProjection> Sets { get; } = [];

    public Task UpsertSessionAsync(TrainingSessionProjection session, IReadOnlyList<TrainingSetProjection> sets, CancellationToken ct = default)
    {
        Sessions[session.SessionId] = session;
        foreach (var id in Sets.Values.Where(s => s.SessionId == session.SessionId).Select(s => s.SetId).ToList())
            Sets.Remove(id);
        foreach (var set in sets)
            Sets[set.SetId] = set;
        return Task.CompletedTask;
    }

    public Task ClearUserAsync(Guid userId, CancellationToken ct = default)
    {
        foreach (var id in Sets.Values.Where(s => s.UserId == userId).Select(s => s.SetId).ToList()) Sets.Remove(id);
        foreach (var id in Sessions.Values.Where(s => s.UserId == userId).Select(s => s.SessionId).ToList()) Sessions.Remove(id);
        return Task.CompletedTask;
    }

    public Task<List<TrainingSessionProjection>> GetSessionsAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(Sessions.Values.Where(s => s.UserId == userId).OrderBy(s => s.StartedAt).ToList());

    public Task<List<TrainingSetProjection>> GetSetsAsync(Guid userId, Guid? exerciseId = null, CancellationToken ct = default)
        => Task.FromResult(Sets.Values
            .Where(s => s.UserId == userId && (exerciseId == null || s.ExerciseId == exerciseId))
            .OrderBy(s => s.SessionId).ThenBy(s => s.Order)
            .ToList());
}

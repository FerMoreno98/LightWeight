using LightWeight.Training.Application.Projections.UserNames;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Training.Infrastructure.Persistence;

public sealed class UserNameProjectionStore : IUserNameProjectionStore
{
    private readonly TrainingDbContext _context;

    public UserNameProjectionStore(TrainingDbContext context)
        => _context = context;

    // Find by id: overwrite if it exists, add it otherwise
    public async Task UpsertAsync(UserNameProjection projection, CancellationToken ct = default)
    {
        UserNameProjection? existing = await _context.UserNames.FindAsync([projection.UserId], ct);
        if (existing is null)
            _context.UserNames.Add(projection);
        else
            _context.Entry(existing).CurrentValues.SetValues(projection);

        await _context.SaveChangesAsync(ct);
    }

    public Task<string?> GetNameAsync(Guid userId, CancellationToken ct = default)
        => _context.UserNames.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (string?)p.Name)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        List<Guid> ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await _context.UserNames.AsNoTracking()
            .Where(p => ids.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => p.Name, ct);
    }
}

using LightWeight.Dashboard.Application.Projections.UserProfile;
using Microsoft.EntityFrameworkCore;

namespace LightWeight.Dashboard.Infrastructure.Persistence;

public sealed class UserProfileProjectionStore : IUserProfileProjectionStore
{
    private readonly DashboardDbContext _context;

    public UserProfileProjectionStore(DashboardDbContext context)
        => _context = context;

    // Find by id: overwrite if it exists, add it otherwise
    public async Task UpsertAsync(UserProfileProjection projection, CancellationToken ct = default)
    {
        UserProfileProjection? existing = await _context.UserProfiles.FindAsync([projection.UserId], ct);
        if (existing is null)
            _context.UserProfiles.Add(projection);
        else
            _context.Entry(existing).CurrentValues.SetValues(projection);

        await _context.SaveChangesAsync(ct);
    }

    public Task<string?> GetSexAsync(Guid userId, CancellationToken ct = default)
        => _context.UserProfiles.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (string?)p.Sex)
            .FirstOrDefaultAsync(ct);
}

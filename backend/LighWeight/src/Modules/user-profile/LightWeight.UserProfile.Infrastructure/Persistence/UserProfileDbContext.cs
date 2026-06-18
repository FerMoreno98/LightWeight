using LightWeight.UserProfile.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;


namespace LightWeight.UserProfile.Infrastructure.Persistence;

public class UserProfileDbContext(DbContextOptions<UserProfileDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("userprofile");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserProfileDbContext).Assembly);
    }
}
using LightWeight.Dashboard.Application.Projections.UserProfile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LightWeight.Dashboard.Infrastructure.Configurations;

// The key is the UserId of the profile (never generated here)
public class UserProfileProjectionConfiguration : IEntityTypeConfiguration<UserProfileProjection>
{
    public void Configure(EntityTypeBuilder<UserProfileProjection> builder)
    {
        builder.ToTable("dashboard_UserProfiles");
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.UserId).ValueGeneratedNever();
        builder.Property(p => p.Sex).HasMaxLength(20).IsRequired();
    }
}

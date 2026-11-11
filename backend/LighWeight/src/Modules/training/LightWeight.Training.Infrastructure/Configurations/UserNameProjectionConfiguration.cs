using LightWeight.Training.Application.Projections.UserNames;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LightWeight.Training.Infrastructure.Configurations;

// The key is the UserId of the profile (never generated here)
public class UserNameProjectionConfiguration : IEntityTypeConfiguration<UserNameProjection>
{
    public void Configure(EntityTypeBuilder<UserNameProjection> builder)
    {
        builder.ToTable("training_UserNames");
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.UserId).ValueGeneratedNever();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
    }
}

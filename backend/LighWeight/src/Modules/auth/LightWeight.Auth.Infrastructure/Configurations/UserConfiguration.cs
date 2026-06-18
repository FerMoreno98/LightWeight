using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Auth.Domain.Aggregates;

namespace LightWeight.Auth.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("auth_Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("Id");
        builder.Property(u => u.Email).HasColumnName("Email").IsRequired();

        builder.Ignore(u => u.DomainEvents);

        builder.HasMany(u => u.DeviceTokens)
            .WithOne()
            .HasForeignKey(d => d.UserId)
            .HasPrincipalKey(u => u.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.OtpCodes)
            .WithOne()
            .HasForeignKey(o => o.UserId)
            .HasPrincipalKey(u => u.Id)
            .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(u => u.OtpCodes)
    .UsePropertyAccessMode(PropertyAccessMode.Field);

    }
}
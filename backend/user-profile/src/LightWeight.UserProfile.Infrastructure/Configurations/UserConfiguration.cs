using LightWeight.UserProfile.Domain.Aggregates;
using LightWeight.UserProfile.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LightWeight.UserProfile.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("userprofile_Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("Id");

        builder.Property(u => u.Name)
            .HasColumnName("Name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.Sex)
            .HasColumnName("Sex")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(
                s => s.ToString(),
                s => Enum.Parse<Sex>(s));

        builder.Property(u => u.DateOfBirth)
            .HasColumnName("DateOfBirth")
            .IsRequired();

        builder.Property(u => u.CurrentStage)
            .HasColumnName("CurrentStage")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(
                s => s.ToString(),
                s => Enum.Parse<TrainingStage>(s));

        builder.Property(u => u.StageStartedAt)
            .HasColumnName("StageStartedAt")
            .IsRequired();

        builder.Ignore(u => u.DomainEvents);
    }
}
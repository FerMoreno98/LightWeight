using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Infrastructure.Configurations;

public class MacrocycleConfiguration : IEntityTypeConfiguration<Macrocycle>
{
    public void Configure(EntityTypeBuilder<Macrocycle> builder)
    {
        builder.ToTable("training_Macrocycles");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(m => m.UserId).HasColumnName("UserId").IsRequired();
        builder.HasIndex(m => m.UserId).HasDatabaseName("Ix_Macrocycle_UserId");
        // A user has at most one active (not finished) macrocycle
        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("Ux_Macrocycle_ActivePerUser")
            .IsUnique()
            .HasFilter("\"FinishedAt\" IS NULL");
        builder.Property(m => m.StartedAt).HasColumnName("StartedAt").IsRequired();
        builder.Property(m => m.FinishedAt).HasColumnName("FinishedAt");
        builder.Ignore(m => m.IsFinished);
        builder.Property(m => m.Stage)
            .HasColumnName("Stage")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(s => s.ToString(), s => Enum.Parse<TrainingStage>(s));
        builder.Property(m => m.Comments).HasColumnName("Comments");
        builder.Ignore(m => m.DomainEvents);
    }
}

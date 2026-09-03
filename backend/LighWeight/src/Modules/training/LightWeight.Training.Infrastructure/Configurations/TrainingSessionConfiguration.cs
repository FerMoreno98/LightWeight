using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;

namespace LightWeight.Training.Infrastructure.Configurations;

public class TrainingSessionConfiguration : IEntityTypeConfiguration<TrainingSession>
{
    public void Configure(EntityTypeBuilder<TrainingSession> builder)
    {
        builder.ToTable("training_TrainingSessions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(t => t.MicrocycleId).HasColumnName("MicrocycleId").IsRequired();
        builder.Property(t => t.UserId).HasColumnName("UserId").IsRequired();
        builder.HasIndex(t => t.UserId).HasDatabaseName("Ix_TrainingSession_UserId");
        builder.Property(t => t.Name).HasColumnName("Name").HasMaxLength(200).IsRequired();
        builder.Property(t => t.StartAt).HasColumnName("StartAt").IsRequired();
        builder.Property(t => t.Duration).HasColumnName("Duration").HasColumnType("interval");
        builder.Property(t => t.Comments).HasColumnName("Comments");
        builder.Property(t => t.MotivationLevel).HasColumnName("MotivationLevel").IsRequired();
        builder.Property(t => t.SleepLevel).HasColumnName("SleepLevel").IsRequired();
        builder.Property(t => t.DOMSLevel).HasColumnName("DOMSLevel").IsRequired();
        builder.Ignore(t => t.DomainEvents);

        builder.HasMany(t => t.Sets)
            .WithOne()
            .HasForeignKey("TrainingSessionId")
            .HasPrincipalKey(t => t.Id)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Sets)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Microcycle>()
            .WithMany()
            .HasForeignKey(t => t.MicrocycleId)
            .HasPrincipalKey(m => m.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using LightWeight.Dashboard.Application.Projections.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LightWeight.Dashboard.Infrastructure.Configurations;

// Keys are the ids of training (never generated here). Muscle groups are PostgreSQL text arrays

public class TrainingSessionProjectionConfiguration : IEntityTypeConfiguration<TrainingSessionProjection>
{
    public void Configure(EntityTypeBuilder<TrainingSessionProjection> builder)
    {
        builder.ToTable("dashboard_TrainingSessions");
        builder.HasKey(s => s.SessionId);
        builder.Property(s => s.SessionId).ValueGeneratedNever();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(s => new { s.UserId, s.StartedAt }).HasDatabaseName("Ix_TrainingSessions_UserId_StartedAt");
    }
}

public class TrainingSetProjectionConfiguration : IEntityTypeConfiguration<TrainingSetProjection>
{
    public void Configure(EntityTypeBuilder<TrainingSetProjection> builder)
    {
        builder.ToTable("dashboard_TrainingSets");
        builder.HasKey(s => s.SetId);
        builder.Property(s => s.SetId).ValueGeneratedNever();
        builder.Property(s => s.ExerciseName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Weight).HasColumnType("decimal(8,2)");
        builder.Property(s => s.Rpe).HasColumnType("decimal(3,1)");
        builder.Property(s => s.Technique).HasMaxLength(20);
        builder.Property(s => s.ExerciseMuscleGroups).HasColumnType("text[]").IsRequired();
        builder.Property(s => s.PlannedMuscleGroups).HasColumnType("text[]");
        builder.HasIndex(s => new { s.UserId, s.ExerciseId }).HasDatabaseName("Ix_TrainingSets_UserId_ExerciseId");
        builder.HasIndex(s => s.SessionId).HasDatabaseName("Ix_TrainingSets_SessionId");
    }
}

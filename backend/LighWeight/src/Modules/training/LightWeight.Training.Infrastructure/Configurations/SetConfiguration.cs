using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Infrastructure.Configurations;

public class SetConfiguration : IEntityTypeConfiguration<Set>
{
    public void Configure(EntityTypeBuilder<Set> builder)
    {
        builder.ToTable("training_Sets");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(s => s.ExerciseId).HasColumnName("ExerciseId").IsRequired();
        builder.Property(s => s.Repetitions).HasColumnName("Repetitions").IsRequired();
        builder.Property(s => s.IsBodyWeight).HasColumnName("IsBodyWeight").IsRequired();
        builder.Property(s => s.Weight).HasColumnName("Weight").HasColumnType("decimal(8,2)").IsRequired();
        builder.Property(s => s.RPE).HasColumnName("RPE").HasColumnType("decimal(3,1)").IsRequired();
        builder.Property(s => s.SuperSetGroupId).HasColumnName("SuperSetGroupId");
        builder.Property(s => s.TemplateSetId).HasColumnName("TemplateSetId");
        builder.HasIndex(s => s.ExerciseId).HasDatabaseName("Ix_Set_ExerciseId");
        builder.ComplexProperty(s => s.AdvanceTrainingTechniques, a =>
        {
            a.Property(x => x.IsDropSet).HasColumnName("AdvanceTrainingTechniques_IsDropSet").IsRequired();
            a.Property(x => x.IsCluster).HasColumnName("AdvanceTrainingTechniques_IsCluster").IsRequired();
            a.Property(x => x.IsMyoRep).HasColumnName("AdvanceTrainingTechniques_IsMyoRep").IsRequired();
        });

        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey(s => s.ExerciseId)
            .HasPrincipalKey(e => e.Id)
            .OnDelete(DeleteBehavior.Cascade);

        // Templates are soft deleted, a hard delete must never remove the history
        builder.HasOne<TemplateSet>()
            .WithMany()
            .HasForeignKey(s => s.TemplateSetId)
            .HasPrincipalKey(ts => ts.Id)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

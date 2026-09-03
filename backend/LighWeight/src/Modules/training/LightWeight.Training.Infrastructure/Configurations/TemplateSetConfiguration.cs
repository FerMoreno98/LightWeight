using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.ValueObjects;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Infrastructure.Configurations;

public class TemplateSetConfiguration : IEntityTypeConfiguration<TemplateSet>
{
    private static readonly ValueConverter<List<MuscleGroups>, string> _musclegroupConverter = new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<List<MuscleGroups>>(v, (JsonSerializerOptions?)null)!
    );

    public void Configure(EntityTypeBuilder<TemplateSet> builder)
    {
        builder.ToTable("training_TemplateSets");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(t => t.ExerciseId).HasColumnName("ExerciseId").IsRequired();
        builder.Property(t => t.ExpectedRIR).HasColumnName("ExpectedRIR").IsRequired();
        builder.Property(t => t.SuperSetGroupId).HasColumnName("SuperSetGroupId");
        builder.Property(t => t.AimMuscleGroups)
        .HasColumnName("AimMuscleGroups")
        .HasColumnType("jsonb")
        .HasConversion(_musclegroupConverter);
        builder.ComplexProperty(t => t.RepetitionRange, r =>
        {
            r.Property(x => x.Min).HasColumnName("RepetitionRange_Min").IsRequired();
            r.Property(x => x.Max).HasColumnName("RepetitionRange_Max").IsRequired();
        });
        builder.ComplexProperty(t => t.AdvanceTrainingTechniques, a =>
        {
            a.Property(x => x.IsDropSet).HasColumnName("AdvanceTrainingTechniques_IsDropSet").IsRequired();
            a.Property(x => x.IsCluster).HasColumnName("AdvanceTrainingTechniques_IsCluster").IsRequired();
            a.Property(x => x.IsMyoRep).HasColumnName("AdvanceTrainingTechniques_IsMyoRep").IsRequired();
        });

        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey(t => t.ExerciseId)
            .HasPrincipalKey(e => e.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

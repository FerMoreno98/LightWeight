using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Infrastructure.Configurations;

public class TemplateSetConfiguration : IEntityTypeConfiguration<TemplateSet>
{
    private static readonly ValueConverter<AdvanceTrainingTechniques, string> _techniquesConverter = new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<AdvanceTrainingTechniques>(v, (JsonSerializerOptions?)null)!
    );

    private static readonly ValueConverter<RepetitionRange, string> _rangeConverter = new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<RepetitionRange>(v, (JsonSerializerOptions?)null)!
    );

    public void Configure(EntityTypeBuilder<TemplateSet> builder)
    {
        builder.ToTable("training_TemplateSets");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(t => t.ExerciseId).HasColumnName("ExerciseId").IsRequired();
        builder.Property(t => t.ExpectedRIR).HasColumnName("ExpectedRIR").IsRequired();
        builder.Property(t => t.SuperSetGroupId).HasColumnName("SuperSetGroupId");
        builder.Property(t => t.RepetitionRange)
            .HasColumnName("RepetitionRange")
            .HasColumnType("jsonb")
            .HasConversion(_rangeConverter);
        builder.Property(t => t.AdvanceTrainingTechniques)
            .HasColumnName("AdvanceTrainingTechniques")
            .HasColumnType("jsonb")
            .HasConversion(_techniquesConverter);

        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey(t => t.ExerciseId)
            .HasPrincipalKey(e => e.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.ValueObjects;

namespace LightWeight.Training.Infrastructure.Configurations;

public class SetConfiguration : IEntityTypeConfiguration<Set>
{
    private static readonly ValueConverter<AdvanceTrainingTechniques, string> _techniquesConverter = new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<AdvanceTrainingTechniques>(v, (JsonSerializerOptions?)null)!
    );

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
        builder.Property(s => s.AdvanceTrainingTechniques)
            .HasColumnName("AdvanceTrainingTechniques")
            .HasColumnType("jsonb")
            .HasConversion(_techniquesConverter);

        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey(s => s.ExerciseId)
            .HasPrincipalKey(e => e.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

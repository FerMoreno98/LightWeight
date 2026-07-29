using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Infrastructure.Configurations;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    private static readonly ValueConverter<List<MuscleGroups>, string> _aimMuscleGroupsConverter = new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<List<MuscleGroups>>(v, (JsonSerializerOptions?)null) ?? new()
    );

    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("training_Exercises");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(e => e.Name).HasColumnName("Name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.IsBilateral).HasColumnName("IsBilateral").IsRequired();
        builder.Property(typeof(List<MuscleGroups>), "_aimMuscleGroups")
            .HasColumnName("AimMuscleGroups")
            .HasColumnType("jsonb")
            .HasConversion(_aimMuscleGroupsConverter)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(e => e.DomainEvents);
    }
}

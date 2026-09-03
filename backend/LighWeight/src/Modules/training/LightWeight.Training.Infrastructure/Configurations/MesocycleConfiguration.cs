using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Infrastructure.Configurations;

public class MesocycleConfiguration : IEntityTypeConfiguration<Mesocycle>
{
    private static readonly ValueConverter<List<MuscleGroups>, string> _aimMuscleGroupsConverter = new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<List<MuscleGroups>>(v, (JsonSerializerOptions?)null) ?? new()
    );

    public void Configure(EntityTypeBuilder<Mesocycle> builder)
    {
        builder.ToTable("training_Mesocycles");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(m => m.MacrocycleId).HasColumnName("MacrocycleId").IsRequired();
        builder.Property(m => m.UserId).HasColumnName("UserId").IsRequired();
        builder.HasIndex(m => m.UserId).HasDatabaseName("Ix_Mesocycle_UserId");
        builder.Property(typeof(List<MuscleGroups>), "_aimMuscleGroups")
            .HasColumnName("AimMuscleGroups")
            .HasColumnType("jsonb")
            .HasConversion(_aimMuscleGroupsConverter)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Property(m => m.MotivationLevel).HasColumnName("MotivationLevel").IsRequired();
        builder.Property(m => m.Injuries).HasColumnName("Injuries");
        builder.Property(m => m.Comments).HasColumnName("Comments");
        builder.Property(m => m.StartAt).HasColumnName("StartAt").IsRequired();
        builder.Property(m => m.EndAt).HasColumnName("EndAt").IsRequired();
        builder.Ignore(m => m.DomainEvents);

        builder.HasOne<Macrocycle>()
            .WithMany()
            .HasForeignKey(m => m.MacrocycleId)
            .HasPrincipalKey(m => m.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

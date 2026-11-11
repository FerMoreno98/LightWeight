using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Infrastructure.Configurations;

public class ProgramConfiguration : IEntityTypeConfiguration<Program>
{
    private static readonly ValueConverter<List<MuscleGroups>, string> _aimMuscleGroupsConverter = new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<List<MuscleGroups>>(v, (JsonSerializerOptions?)null) ?? new()
    );

    public void Configure(EntityTypeBuilder<Program> builder)
    {
        builder.ToTable("training_Programs");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(p => p.UserId).HasColumnName("UserId").IsRequired();
        builder.HasIndex(p => p.UserId).HasDatabaseName("Ix_Program_UserId");
        builder.Property(p => p.Name).HasColumnName("Name").HasMaxLength(200).IsRequired();
        builder.Property(p => p.Periodization)
            .HasColumnName("Periodization")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(s => s.ToString(), s => Enum.Parse<Periodization>(s));
        builder.Property(p => p.AimMuscleGroups)
            .HasColumnName("AimMuscleGroups")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(_aimMuscleGroupsConverter);
        // Sharing: the token is unique while set (partial unique index Ux_Program_ShareToken, created in the migration)
        builder.Property(p => p.ShareToken).HasColumnName("ShareToken").HasMaxLength(Program.ShareTokenLength);
        builder.HasIndex(p => p.ShareToken)
            .HasDatabaseName("Ux_Program_ShareToken")
            .IsUnique()
            .HasFilter("\"ShareToken\" IS NOT NULL");
        // Origin of a copied program. No FKs: the original program (or its author) can be deleted
        builder.Property(p => p.CopiedFromProgramId).HasColumnName("CopiedFromProgramId");
        builder.Property(p => p.CopiedFromUserId).HasColumnName("CopiedFromUserId");
        builder.Ignore(p => p.DomainEvents);

        builder.HasMany(p => p.trainingTemplates)
            .WithOne()
            .HasForeignKey("ProgramId")
            .IsRequired()
            .HasPrincipalKey(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.trainingTemplates)
            .HasField("_trainingTemplates")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

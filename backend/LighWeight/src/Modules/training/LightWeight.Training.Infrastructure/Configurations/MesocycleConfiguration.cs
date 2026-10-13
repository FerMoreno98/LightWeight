using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;

namespace LightWeight.Training.Infrastructure.Configurations;

public class MesocycleConfiguration : IEntityTypeConfiguration<Mesocycle>
{
    public void Configure(EntityTypeBuilder<Mesocycle> builder)
    {
        builder.ToTable("training_Mesocycles");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(m => m.MacrocycleId).HasColumnName("MacrocycleId").IsRequired();
        // A macrocycle has at most one active (not finished) mesocycle
        builder.HasIndex(m => m.MacrocycleId)
            .HasDatabaseName("Ux_Mesocycle_ActivePerMacrocycle")
            .IsUnique()
            .HasFilter("\"FinishedAt\" IS NULL");
        builder.Property(m => m.UserId).HasColumnName("UserId").IsRequired();
        builder.HasIndex(m => m.UserId).HasDatabaseName("Ix_Mesocycle_UserId");
        builder.Property(m => m.ProgramId).HasColumnName("ProgramId").IsRequired();
        builder.Property(m => m.MotivationLevel).HasColumnName("MotivationLevel").IsRequired();
        builder.Property(m => m.Injuries).HasColumnName("Injuries");
        builder.Property(m => m.Comments).HasColumnName("Comments");
        builder.Property(m => m.StartedAt).HasColumnName("StartedAt").IsRequired();
        builder.Property(m => m.FinishedAt).HasColumnName("FinishedAt");
        builder.Ignore(m => m.IsFinished);
        builder.Ignore(m => m.ActiveMicrocycle);

        builder.HasMany(m => m.Microcycles)
            .WithOne()
            .HasForeignKey(m => m.MesocycleId)
            .IsRequired()
            .HasPrincipalKey(m => m.Id)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(m => m.Microcycles)
            .HasField("_microcycles")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // A program followed by a mesocycle cannot be removed
        builder.HasOne<Program>()
            .WithMany()
            .HasForeignKey(m => m.ProgramId)
            .HasPrincipalKey(p => p.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

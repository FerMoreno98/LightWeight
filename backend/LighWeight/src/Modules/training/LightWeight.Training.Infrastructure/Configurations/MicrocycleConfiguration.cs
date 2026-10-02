using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Entities;

namespace LightWeight.Training.Infrastructure.Configurations;

public class MicrocycleConfiguration : IEntityTypeConfiguration<Microcycle>
{
    public void Configure(EntityTypeBuilder<Microcycle> builder)
    {
        builder.ToTable("training_Microcycles");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(m => m.MesocycleId).HasColumnName("MesocycleId").IsRequired();
        builder.Property(m => m.UserId).HasColumnName("UserId").IsRequired();
        builder.HasIndex(m => m.UserId).HasDatabaseName("Ix_Microcycle_UserId");
        builder.Property(m => m.TrainingTemplateId).HasColumnName("TrainingTemplateId").IsRequired();
        builder.Property(m => m.WeekNumber).HasColumnName("WeekNumber").IsRequired();
        builder.Ignore(m => m.DomainEvents);

        builder.HasOne<Mesocycle>()
            .WithMany()
            .HasForeignKey(m => m.MesocycleId)
            .HasPrincipalKey(m => m.Id)
            .OnDelete(DeleteBehavior.Cascade);

        // Templates are soft deleted, a hard delete must never remove the history
        builder.HasOne<TrainingTemplate>()
            .WithMany()
            .HasForeignKey(m => m.TrainingTemplateId)
            .HasPrincipalKey(t => t.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

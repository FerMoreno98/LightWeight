using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Aggregates;
using LightWeight.Training.Domain.Enum;

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
        builder.Property(m => m.DurationInDays).HasColumnName("DurationInDays").IsRequired();
        builder.Property(m => m.TrainingDistribution)
            .HasColumnName("TrainingDistribution")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(s => s.ToString(), s => Enum.Parse<TrainingDistribution>(s));
        builder.Ignore(m => m.DomainEvents);

        builder.HasOne<Mesocycle>()
            .WithMany()
            .HasForeignKey(m => m.MesocycleId)
            .HasPrincipalKey(m => m.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

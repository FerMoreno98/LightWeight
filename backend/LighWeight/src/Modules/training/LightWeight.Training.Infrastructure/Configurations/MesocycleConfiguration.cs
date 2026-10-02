using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Aggregates;

namespace LightWeight.Training.Infrastructure.Configurations;

public class MesocycleConfiguration : IEntityTypeConfiguration<Mesocycle>
{
    public void Configure(EntityTypeBuilder<Mesocycle> builder)
    {
        builder.ToTable("training_Mesocycles");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(m => m.MacrocycleId).HasColumnName("MacrocycleId").IsRequired();
        builder.Property(m => m.UserId).HasColumnName("UserId").IsRequired();
        builder.HasIndex(m => m.UserId).HasDatabaseName("Ix_Mesocycle_UserId");
        builder.Property(m => m.ProgramId).HasColumnName("ProgramId").IsRequired();
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

        // A program followed by a mesocycle cannot be removed
        builder.HasOne<Program>()
            .WithMany()
            .HasForeignKey(m => m.ProgramId)
            .HasPrincipalKey(p => p.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

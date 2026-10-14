using LightWeight.BodyMetrics.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LightWeight.BodyMetrics.Infrastructure.Configurations;

public class PerimetersConfiguration : IEntityTypeConfiguration<Perimeters>
{
    public void Configure(EntityTypeBuilder<Perimeters> builder)
    {
        builder.ToTable("bodymetrics_Perimeters");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(p => p.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(p => p.MeasuredAt).HasColumnName("MeasuredAt").IsRequired();
        builder.HasIndex(p => new { p.UserId, p.MeasuredAt }).HasDatabaseName("Ix_Perimeters_UserId_MeasuredAt");
        builder.Ignore(p => p.DomainEvents);

        // The value object is stored in the same row, one nullable column per perimeter (cm)
        builder.ComplexProperty(p => p.Measures, m =>
        {
            m.Property(x => x.Neck).HasColumnName("Neck").HasColumnType("decimal(5,2)");
            m.Property(x => x.RightArmRelaxed).HasColumnName("RightArmRelaxed").HasColumnType("decimal(5,2)");
            m.Property(x => x.RightArmTensioned).HasColumnName("RightArmTensioned").HasColumnType("decimal(5,2)");
            m.Property(x => x.LeftArmRelaxed).HasColumnName("LeftArmRelaxed").HasColumnType("decimal(5,2)");
            m.Property(x => x.LeftArmTensioned).HasColumnName("LeftArmTensioned").HasColumnType("decimal(5,2)");
            m.Property(x => x.Chest).HasColumnName("Chest").HasColumnType("decimal(5,2)");
            m.Property(x => x.Shoulder).HasColumnName("Shoulder").HasColumnType("decimal(5,2)");
            m.Property(x => x.Waist).HasColumnName("Waist").HasColumnType("decimal(5,2)");
            m.Property(x => x.Hip).HasColumnName("Hip").HasColumnType("decimal(5,2)");
            m.Property(x => x.Abs).HasColumnName("Abs").HasColumnType("decimal(5,2)");
            m.Property(x => x.RightThigh).HasColumnName("RightThigh").HasColumnType("decimal(5,2)");
            m.Property(x => x.LeftThigh).HasColumnName("LeftThigh").HasColumnType("decimal(5,2)");
            m.Property(x => x.RightCalf).HasColumnName("RightCalf").HasColumnType("decimal(5,2)");
            m.Property(x => x.LeftCalf).HasColumnName("LeftCalf").HasColumnType("decimal(5,2)");
        });
    }
}

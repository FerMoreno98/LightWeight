using LightWeight.BodyMetrics.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LightWeight.BodyMetrics.Infrastructure.Configurations;

public class SkinFoldsConfiguration : IEntityTypeConfiguration<SkinFolds>
{
    public void Configure(EntityTypeBuilder<SkinFolds> builder)
    {
        builder.ToTable("bodymetrics_SkinFolds");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(s => s.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(s => s.MeasuredAt).HasColumnName("MeasuredAt").IsRequired();
        builder.HasIndex(s => new { s.UserId, s.MeasuredAt }).HasDatabaseName("Ix_SkinFolds_UserId_MeasuredAt");
        builder.Ignore(s => s.DomainEvents);

        // The value object is stored in the same row, one nullable column per skinfold (mm).
        // The summatory is calculated from them, so it is not stored
        builder.ComplexProperty(s => s.Measures, m =>
        {
            m.Property(x => x.Abdominal).HasColumnName("Abdominal").HasColumnType("decimal(5,2)");
            m.Property(x => x.Suprailiac).HasColumnName("Suprailiac").HasColumnType("decimal(5,2)");
            m.Property(x => x.Tricipital).HasColumnName("Tricipital").HasColumnType("decimal(5,2)");
            m.Property(x => x.Subscapular).HasColumnName("Subscapular").HasColumnType("decimal(5,2)");
            m.Property(x => x.Thigh).HasColumnName("Thigh").HasColumnType("decimal(5,2)");
            m.Property(x => x.Calf).HasColumnName("Calf").HasColumnType("decimal(5,2)");
        });
    }
}

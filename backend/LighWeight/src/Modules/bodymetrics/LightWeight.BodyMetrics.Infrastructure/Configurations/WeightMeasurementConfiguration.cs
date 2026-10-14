using LightWeight.BodyMetrics.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LightWeight.BodyMetrics.Infrastructure.Configurations;

public class WeightMeasurementConfiguration : IEntityTypeConfiguration<WeightMeasurement>
{
    public void Configure(EntityTypeBuilder<WeightMeasurement> builder)
    {
        builder.ToTable("bodymetrics_WeightMeasurements");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(w => w.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(w => w.Weight).HasColumnName("Weight").HasColumnType("decimal(5,2)").IsRequired();
        builder.Property(w => w.MeasuredAt).HasColumnName("MeasuredAt").IsRequired();
        // The history of a user is always read by date
        builder.HasIndex(w => new { w.UserId, w.MeasuredAt }).HasDatabaseName("Ix_WeightMeasurement_UserId_MeasuredAt");
        builder.Ignore(w => w.DomainEvents);
    }
}

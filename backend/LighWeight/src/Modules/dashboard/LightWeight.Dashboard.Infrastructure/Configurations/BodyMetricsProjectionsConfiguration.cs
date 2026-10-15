using LightWeight.Dashboard.Application.Projections.BodyMetrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LightWeight.Dashboard.Infrastructure.Configurations;

// The key is the MeasurementId of bodymetrics (never generated here) and the charts read by user and date

public class WeightProjectionConfiguration : IEntityTypeConfiguration<WeightProjection>
{
    public void Configure(EntityTypeBuilder<WeightProjection> builder)
    {
        builder.ToTable("dashboard_BodyWeights");
        builder.HasKey(w => w.MeasurementId);
        builder.Property(w => w.MeasurementId).ValueGeneratedNever();
        builder.Property(w => w.UserId).IsRequired();
        builder.Property(w => w.MeasuredAt).IsRequired();
        builder.Property(w => w.Weight).HasColumnType("decimal(5,2)").IsRequired();
        builder.HasIndex(w => new { w.UserId, w.MeasuredAt }).HasDatabaseName("Ix_BodyWeights_UserId_MeasuredAt");
    }
}

public class PerimetersProjectionConfiguration : IEntityTypeConfiguration<PerimetersProjection>
{
    public void Configure(EntityTypeBuilder<PerimetersProjection> builder)
    {
        builder.ToTable("dashboard_BodyPerimeters");
        builder.HasKey(p => p.MeasurementId);
        builder.Property(p => p.MeasurementId).ValueGeneratedNever();
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.MeasuredAt).IsRequired();
        builder.Property(p => p.Neck).HasColumnType("decimal(5,2)");
        builder.Property(p => p.RightArmRelaxed).HasColumnType("decimal(5,2)");
        builder.Property(p => p.RightArmTensioned).HasColumnType("decimal(5,2)");
        builder.Property(p => p.LeftArmRelaxed).HasColumnType("decimal(5,2)");
        builder.Property(p => p.LeftArmTensioned).HasColumnType("decimal(5,2)");
        builder.Property(p => p.Chest).HasColumnType("decimal(5,2)");
        builder.Property(p => p.Shoulder).HasColumnType("decimal(5,2)");
        builder.Property(p => p.Waist).HasColumnType("decimal(5,2)");
        builder.Property(p => p.Hip).HasColumnType("decimal(5,2)");
        builder.Property(p => p.Abs).HasColumnType("decimal(5,2)");
        builder.Property(p => p.RightThigh).HasColumnType("decimal(5,2)");
        builder.Property(p => p.LeftThigh).HasColumnType("decimal(5,2)");
        builder.Property(p => p.RightCalf).HasColumnType("decimal(5,2)");
        builder.Property(p => p.LeftCalf).HasColumnType("decimal(5,2)");
        builder.HasIndex(p => new { p.UserId, p.MeasuredAt }).HasDatabaseName("Ix_BodyPerimeters_UserId_MeasuredAt");
    }
}

public class SkinFoldsProjectionConfiguration : IEntityTypeConfiguration<SkinFoldsProjection>
{
    public void Configure(EntityTypeBuilder<SkinFoldsProjection> builder)
    {
        builder.ToTable("dashboard_BodySkinFolds");
        builder.HasKey(s => s.MeasurementId);
        builder.Property(s => s.MeasurementId).ValueGeneratedNever();
        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.MeasuredAt).IsRequired();
        builder.Property(s => s.Abdominal).HasColumnType("decimal(5,2)");
        builder.Property(s => s.Suprailiac).HasColumnType("decimal(5,2)");
        builder.Property(s => s.Tricipital).HasColumnType("decimal(5,2)");
        builder.Property(s => s.Subscapular).HasColumnType("decimal(5,2)");
        builder.Property(s => s.Thigh).HasColumnType("decimal(5,2)");
        builder.Property(s => s.Calf).HasColumnType("decimal(5,2)");
        builder.Property(s => s.Summatory).HasColumnType("decimal(5,2)");
        builder.HasIndex(s => new { s.UserId, s.MeasuredAt }).HasDatabaseName("Ix_BodySkinFolds_UserId_MeasuredAt");
    }
}

using LightWeight.Dashboard.Application.Projections.Nutrition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LightWeight.Dashboard.Infrastructure.Configurations;

// The key is the DiaryDayId of nutrition (never generated here)
public class NutritionDayProjectionConfiguration : IEntityTypeConfiguration<NutritionDayProjection>
{
    public void Configure(EntityTypeBuilder<NutritionDayProjection> builder)
    {
        builder.ToTable("dashboard_NutritionDays");
        builder.HasKey(d => d.DiaryDayId);
        builder.Property(d => d.DiaryDayId).ValueGeneratedNever();
        builder.HasIndex(d => new { d.UserId, d.Date }).HasDatabaseName("Ix_NutritionDays_UserId_Date");
        builder.Property(d => d.Calories).HasPrecision(7, 2);
        builder.Property(d => d.Protein).HasPrecision(6, 2);
        builder.Property(d => d.Carbohydrate).HasPrecision(6, 2);
        builder.Property(d => d.Fat).HasPrecision(6, 2);
    }
}

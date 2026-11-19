using LightWeight.Nutrition.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LightWeight.Nutrition.Infrastructure.Configurations;

public class DiaryDayConfiguration : IEntityTypeConfiguration<DiaryDay>
{
    public void Configure(EntityTypeBuilder<DiaryDay> builder)
    {
        builder.ToTable("nutrition_DiaryDays");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(d => d.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(d => d.Date).HasColumnName("Date").IsRequired();
        // One row per day and user: it is also the index the synchronization and the rebuild read by
        builder.HasIndex(d => new { d.UserId, d.Date }).IsUnique().HasDatabaseName("Ux_DiaryDays_UserId_Date");
        builder.Ignore(d => d.DomainEvents);

        // The value object is stored in the same row
        builder.ComplexProperty(d => d.Intake, i =>
        {
            i.Property(x => x.Calories).HasColumnName("Calories").HasColumnType("decimal(7,2)");
            i.Property(x => x.Protein).HasColumnName("Protein").HasColumnType("decimal(6,2)");
            i.Property(x => x.Carbohydrate).HasColumnName("Carbohydrate").HasColumnType("decimal(6,2)");
            i.Property(x => x.Fat).HasColumnName("Fat").HasColumnType("decimal(6,2)");
        });
    }
}

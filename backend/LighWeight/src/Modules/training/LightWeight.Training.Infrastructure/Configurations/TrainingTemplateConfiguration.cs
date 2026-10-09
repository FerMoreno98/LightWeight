using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Entities;
using LightWeight.Training.Domain.Enum;

namespace LightWeight.Training.Infrastructure.Configurations;

public class TrainingTemplateConfiguration : IEntityTypeConfiguration<TrainingTemplate>
{
    public void Configure(EntityTypeBuilder<TrainingTemplate> builder)
    {
        builder.ToTable("training_TrainingTemplates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(t => t.Name).HasColumnName("Name").HasMaxLength(TrainingTemplate.NameMaxLength).IsRequired();
        builder.Property(v => v.VolumeLandmark)
            .HasColumnName("VolumeLandmark")
            .HasMaxLength(50)
            .HasConversion(s => s.ToString(), s => Enum.Parse<VolumeLandmarks>(s));
        builder.Property(t => t.TrainingDistribution)
            .HasColumnName("TrainingDistribution")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(s => s.ToString(), s => Enum.Parse<TrainingDistribution>(s));
        builder.Property(t => t.DurationInDays).HasColumnName("DurationInDays").IsRequired();
        builder.Property(t => t.Order).HasColumnName("Order").IsRequired();
        builder.Property(t => t.IsDeleted).HasColumnName("IsDeleted").HasDefaultValue(false).IsRequired();
        builder.Property(t => t.DeletedAt).HasColumnName("DeletedAt");
        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasMany(t => t.TemplateSessions)
            .WithOne()
            .HasForeignKey("TrainingTemplateId")
            .IsRequired()
            .HasPrincipalKey(t => t.Id)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.TemplateSessions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

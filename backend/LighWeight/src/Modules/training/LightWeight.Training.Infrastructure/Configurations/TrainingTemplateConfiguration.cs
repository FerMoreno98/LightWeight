using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Aggregates;
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
        builder.Property(t => t.UserId).HasColumnName("UserId").IsRequired();
        builder.Property(t => t.Name).HasColumnName("Name").HasMaxLength(200).IsRequired();
        builder.Property(t => t.TrainingDistribution)
            .HasColumnName("TrainingDistribution")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion(s => s.ToString(), s => Enum.Parse<TrainingDistribution>(s));
        builder.Ignore(t => t.DomainEvents);

        builder.HasMany(t => t.TemplateSessions)
            .WithOne()
            .HasForeignKey("TrainingTemplateId")
            .HasPrincipalKey(t => t.Id)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.TemplateSessions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

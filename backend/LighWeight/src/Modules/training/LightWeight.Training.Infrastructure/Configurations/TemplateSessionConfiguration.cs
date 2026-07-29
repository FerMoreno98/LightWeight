using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LightWeight.Training.Domain.Entities;

namespace LightWeight.Training.Infrastructure.Configurations;

public class TemplateSessionConfiguration : IEntityTypeConfiguration<TemplateSession>
{
    public void Configure(EntityTypeBuilder<TemplateSession> builder)
    {
        builder.ToTable("training_TemplateSessions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("Id").ValueGeneratedNever();
        builder.Property(t => t.Name).HasColumnName("Name").HasMaxLength(200).IsRequired();

        builder.HasMany(t => t.TemplateExercises)
            .WithOne()
            .HasForeignKey("TemplateSessionId")
            .HasPrincipalKey(t => t.Id)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.TemplateExercises)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

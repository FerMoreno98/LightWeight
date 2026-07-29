using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291205)]
public class CreateTemplateSessionTable : Migration
{
    public override void Up()
    {
        Create.Table("training_TemplateSessions").InSchema("training")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("TrainingTemplateId").AsGuid().NotNullable()
                .ForeignKey("Fk_TemplateSession_TrainingTemplate", "training", "training_TrainingTemplates", "Id")
                .OnDelete(System.Data.Rule.Cascade);
    }

    public override void Down()
    {
        Delete.Table("training_TemplateSessions");
    }
}

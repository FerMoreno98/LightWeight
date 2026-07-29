using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291204)]
public class CreateTrainingTemplateTable : Migration
{
    public override void Up()
    {
        Create.Table("training_TrainingTemplates").InSchema("training")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("TrainingDistribution").AsString(20).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("training_TrainingTemplates");
    }
}

using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

/// <summary>
/// Templates get a name so they can be told apart (duplicates included).
/// Existing templates are named after their order ("Plantilla 1", "Plantilla 2"...)
/// </summary>
[Migration(202607291215)]
public class AddNameToTrainingTemplate : Migration
{
    public override void Up()
    {
        Alter.Table("training_TrainingTemplates").InSchema("training")
            .AddColumn("Name").AsString(200).Nullable();

        Execute.Sql(@"
            UPDATE training.""training_TrainingTemplates""
            SET ""Name"" = 'Plantilla ' || ""Order"";");

        Alter.Column("Name").OnTable("training_TrainingTemplates").InSchema("training").AsString(200).NotNullable();
    }

    public override void Down()
    {
        Delete.Column("Name").FromTable("training_TrainingTemplates").InSchema("training");
    }
}

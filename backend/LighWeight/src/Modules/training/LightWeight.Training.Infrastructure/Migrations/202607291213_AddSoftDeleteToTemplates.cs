using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291213)]
public class AddSoftDeleteToTemplates : Migration
{
    private static readonly string[] Tables =
    [
        "training_TrainingTemplates",
        "training_TemplateSessions",
        "training_TemplateSets"
    ];

    public override void Up()
    {
        foreach (var table in Tables)
        {
            Alter.Table(table).InSchema("training")
                .AddColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false)
                .AddColumn("DeletedAt").AsCustom("timestamp with time zone").Nullable();
        }
    }

    public override void Down()
    {
        foreach (var table in Tables)
        {
            Delete.Column("IsDeleted").Column("DeletedAt")
                .FromTable(table).InSchema("training");
        }
    }
}

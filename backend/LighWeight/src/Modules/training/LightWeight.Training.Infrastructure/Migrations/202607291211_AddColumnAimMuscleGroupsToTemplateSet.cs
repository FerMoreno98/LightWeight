using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291211)]
public class AddColumnAimMuscleGroupToTemplateSet : Migration
{
    public override void Down()
    {
        Delete.Column("AimMuscleGroups").FromTable("training_TemplateSets");
    }

    public override void Up()
    {
        Alter.Table("training_TemplateSets").InSchema("training")
            .AddColumn("AimMuscleGroups").AsCustom("jsonb").Nullable();
    }
}
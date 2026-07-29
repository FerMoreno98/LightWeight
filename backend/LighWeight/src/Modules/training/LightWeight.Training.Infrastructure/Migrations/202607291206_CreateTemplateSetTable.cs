using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291206)]
public class CreateTemplateSetTable : Migration
{
    public override void Up()
    {
        Create.Table("training_TemplateSets").InSchema("training")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("TemplateSessionId").AsGuid().NotNullable()
                .ForeignKey("Fk_TemplateSet_TemplateSession", "training", "training_TemplateSessions", "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("ExerciseId").AsGuid().NotNullable()
                .ForeignKey("Fk_TemplateSet_Exercise", "training", "training_Exercises", "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("ExpectedRIR").AsInt32().NotNullable()
            .WithColumn("SuperSetGroupId").AsGuid().Nullable()
            .WithColumn("RepetitionRange").AsCustom("jsonb").NotNullable()
            .WithColumn("AdvanceTrainingTechniques").AsCustom("jsonb").NotNullable();
    }

    public override void Down()
    {
        Delete.Table("training_TemplateSets");
    }
}

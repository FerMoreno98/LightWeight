using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291208)]
public class CreateSetTable : Migration
{
    public override void Up()
    {
        Create.Table("training_Sets").InSchema("training")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("TrainingSessionId").AsGuid().NotNullable()
                .ForeignKey("Fk_Set_TrainingSession", "training", "training_TrainingSessions", "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("ExerciseId").AsGuid().NotNullable()
                .ForeignKey("Fk_Set_Exercise", "training", "training_Exercises", "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("Repetitions").AsInt32().NotNullable()
            .WithColumn("IsBodyWeight").AsBoolean().NotNullable()
            .WithColumn("Weight").AsCustom("decimal(8,2)").NotNullable()
            .WithColumn("RPE").AsCustom("decimal(3,1)").NotNullable()
            .WithColumn("SuperSetGroupId").AsGuid().Nullable()
            .WithColumn("AdvanceTrainingTechniques").AsCustom("jsonb").NotNullable();
    }

    public override void Down()
    {
        Delete.Table("training_Sets");
    }
}

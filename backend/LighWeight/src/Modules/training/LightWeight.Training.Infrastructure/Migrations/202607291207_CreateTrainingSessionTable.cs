using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291207)]
public class CreateTrainingSessionTable : Migration
{
    public override void Up()
    {
        Create.Table("training_TrainingSessions").InSchema("training")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("MicrocycleId").AsGuid().NotNullable()
                .ForeignKey("Fk_TrainingSession_Microcycle", "training", "training_Microcycles", "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("StartAt").AsDateTime().NotNullable()
            .WithColumn("Duration").AsCustom("interval").NotNullable()
            .WithColumn("Comments").AsString().Nullable()
            .WithColumn("MotivationLevel").AsInt32().NotNullable()
            .WithColumn("SleepLevel").AsInt32().NotNullable()
            .WithColumn("DOMSLevel").AsInt32().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("training_TrainingSessions");
    }
}

using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291209)]
public class AddUserIdToTrainingHierarchy : Migration
{
    // added userId to Mesocycle, Microcycle and TrainingSession so that i don't have to bring the entire Macrocycle to pick every single piece
    public override void Up()
    {
        Alter.Table("training_Mesocycles").InSchema("training")
            .AddColumn("UserId").AsGuid().NotNullable();
        Create.Index("Ix_Mesocycle_UserId")
            .OnTable("training_Mesocycles").InSchema("training")
            .OnColumn("UserId");

        Alter.Table("training_Microcycles").InSchema("training")
            .AddColumn("UserId").AsGuid().NotNullable();
        Create.Index("Ix_Microcycle_UserId")
            .OnTable("training_Microcycles").InSchema("training")
            .OnColumn("UserId");

        Alter.Table("training_TrainingSessions").InSchema("training")
            .AddColumn("UserId").AsGuid().NotNullable();
        Create.Index("Ix_TrainingSession_UserId")
            .OnTable("training_TrainingSessions").InSchema("training")
            .OnColumn("UserId");
    }

    public override void Down()
    {
        Delete.Index("Ix_TrainingSession_UserId").OnTable("training_TrainingSessions").InSchema("training");
        Delete.Column("UserId").FromTable("training_TrainingSessions").InSchema("training");

        Delete.Index("Ix_Microcycle_UserId").OnTable("training_Microcycles").InSchema("training");
        Delete.Column("UserId").FromTable("training_Microcycles").InSchema("training");

        Delete.Index("Ix_Mesocycle_UserId").OnTable("training_Mesocycles").InSchema("training");
        Delete.Column("UserId").FromTable("training_Mesocycles").InSchema("training");
    }
}

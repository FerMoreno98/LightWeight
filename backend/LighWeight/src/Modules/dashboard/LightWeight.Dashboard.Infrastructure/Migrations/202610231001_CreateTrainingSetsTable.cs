using FluentMigrator;

namespace LightWeight.Dashboard.Infrastructure.Migrations;

[Migration(202610231001)]
public class CreateTrainingSetsTable : Migration
{
    public override void Up()
    {
        // The sets of each projected session: what was performed and what had been planned for it
        Create.Table("dashboard_TrainingSets").InSchema("dashboard")
            .WithColumn("SetId").AsGuid().PrimaryKey()
            .WithColumn("SessionId").AsGuid().NotNullable()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("Order").AsInt32().NotNullable()
            .WithColumn("ExerciseId").AsGuid().NotNullable()
            .WithColumn("ExerciseName").AsString(200).NotNullable()
            .WithColumn("Repetitions").AsInt32().NotNullable()
            .WithColumn("Weight").AsDecimal(8, 2).NotNullable()
            .WithColumn("Rpe").AsDecimal(3, 1).NotNullable()
            .WithColumn("IsBodyWeight").AsBoolean().NotNullable()
            .WithColumn("Technique").AsString(20).Nullable()
            .WithColumn("IsPlanned").AsBoolean().NotNullable()
            .WithColumn("ExerciseMuscleGroups").AsCustom("text[]").NotNullable()
            .WithColumn("PlannedMuscleGroups").AsCustom("text[]").Nullable()
            .WithColumn("PlannedMinReps").AsInt32().Nullable()
            .WithColumn("PlannedMaxReps").AsInt32().Nullable();

        Create.Index("Ix_TrainingSets_UserId_ExerciseId").OnTable("dashboard_TrainingSets").InSchema("dashboard")
            .OnColumn("UserId").Ascending()
            .OnColumn("ExerciseId").Ascending();
        Create.Index("Ix_TrainingSets_SessionId").OnTable("dashboard_TrainingSets").InSchema("dashboard")
            .OnColumn("SessionId").Ascending();
    }

    public override void Down()
    {
        Delete.Table("dashboard_TrainingSets").InSchema("dashboard");
    }
}

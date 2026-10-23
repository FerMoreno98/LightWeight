using FluentMigrator;

namespace LightWeight.Dashboard.Infrastructure.Migrations;

[Migration(202610231000)]
public class CreateTrainingSessionsTable : Migration
{
    public override void Up()
    {
        // Projection of the finished sessions of training, keyed by its SessionId. The cycle ids are stored so the
        // charts can be filtered by macrocycle / mesocycle with a plain WHERE
        Create.Table("dashboard_TrainingSessions").InSchema("dashboard")
            .WithColumn("SessionId").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("MacrocycleId").AsGuid().NotNullable()
            .WithColumn("MesocycleId").AsGuid().NotNullable()
            .WithColumn("MicrocycleId").AsGuid().NotNullable()
            .WithColumn("WeekNumber").AsInt32().NotNullable()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("StartedAt").AsCustom("timestamp with time zone").NotNullable()
            .WithColumn("FinishedAt").AsCustom("timestamp with time zone").NotNullable()
            .WithColumn("Motivation").AsInt32().NotNullable()
            .WithColumn("Sleep").AsInt32().NotNullable()
            .WithColumn("Doms").AsInt32().NotNullable();

        Create.Index("Ix_TrainingSessions_UserId_StartedAt").OnTable("dashboard_TrainingSessions").InSchema("dashboard")
            .OnColumn("UserId").Ascending()
            .OnColumn("StartedAt").Ascending();
    }

    public override void Down()
    {
        Delete.Table("dashboard_TrainingSessions").InSchema("dashboard");
    }
}

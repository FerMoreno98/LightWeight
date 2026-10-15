using FluentMigrator;

namespace LightWeight.Dashboard.Infrastructure.Migrations;

[Migration(202610151001)]
public class CreateBodyWeightsTable : Migration
{
    public override void Up()
    {
        // Projection of the weights of bodymetrics, keyed by its MeasurementId
        Create.Table("dashboard_BodyWeights").InSchema("dashboard")
            .WithColumn("MeasurementId").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("MeasuredAt").AsCustom("timestamp with time zone").NotNullable()
            .WithColumn("Weight").AsDecimal(5, 2).NotNullable();

        Create.Index("Ix_BodyWeights_UserId_MeasuredAt").OnTable("dashboard_BodyWeights").InSchema("dashboard")
            .OnColumn("UserId").Ascending()
            .OnColumn("MeasuredAt").Ascending();
    }

    public override void Down()
    {
        Delete.Table("dashboard_BodyWeights").InSchema("dashboard");
    }
}

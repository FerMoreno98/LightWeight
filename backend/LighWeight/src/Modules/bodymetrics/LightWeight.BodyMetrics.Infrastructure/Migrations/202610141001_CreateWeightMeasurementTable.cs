using FluentMigrator;

namespace LightWeight.BodyMetrics.Infrastructure.Migrations;

[Migration(202610141001)]
public class CreateWeightMeasurementTable : Migration
{
    public override void Up()
    {
        Create.Table("bodymetrics_WeightMeasurements").InSchema("bodymetrics")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("Weight").AsDecimal(5, 2).NotNullable()
            // Set from DateTime in UTC, so it is stored with time zone
            .WithColumn("MeasuredAt").AsCustom("timestamp with time zone").NotNullable();

        Create.Index("Ix_WeightMeasurement_UserId_MeasuredAt").OnTable("bodymetrics_WeightMeasurements").InSchema("bodymetrics")
            .OnColumn("UserId").Ascending()
            .OnColumn("MeasuredAt").Ascending();
    }

    public override void Down()
    {
        Delete.Table("bodymetrics_WeightMeasurements").InSchema("bodymetrics");
    }
}

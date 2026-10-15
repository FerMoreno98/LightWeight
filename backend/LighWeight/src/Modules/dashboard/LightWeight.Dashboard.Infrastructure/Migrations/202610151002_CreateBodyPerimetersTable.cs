using FluentMigrator;

namespace LightWeight.Dashboard.Infrastructure.Migrations;

[Migration(202610151002)]
public class CreateBodyPerimetersTable : Migration
{
    public override void Up()
    {
        // Projection of the perimeters of bodymetrics: one nullable column per perimeter (cm)
        Create.Table("dashboard_BodyPerimeters").InSchema("dashboard")
            .WithColumn("MeasurementId").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("MeasuredAt").AsCustom("timestamp with time zone").NotNullable()
            .WithColumn("Neck").AsDecimal(5, 2).Nullable()
            .WithColumn("RightArmRelaxed").AsDecimal(5, 2).Nullable()
            .WithColumn("RightArmTensioned").AsDecimal(5, 2).Nullable()
            .WithColumn("LeftArmRelaxed").AsDecimal(5, 2).Nullable()
            .WithColumn("LeftArmTensioned").AsDecimal(5, 2).Nullable()
            .WithColumn("Chest").AsDecimal(5, 2).Nullable()
            .WithColumn("Shoulder").AsDecimal(5, 2).Nullable()
            .WithColumn("Waist").AsDecimal(5, 2).Nullable()
            .WithColumn("Hip").AsDecimal(5, 2).Nullable()
            .WithColumn("Abs").AsDecimal(5, 2).Nullable()
            .WithColumn("RightThigh").AsDecimal(5, 2).Nullable()
            .WithColumn("LeftThigh").AsDecimal(5, 2).Nullable()
            .WithColumn("RightCalf").AsDecimal(5, 2).Nullable()
            .WithColumn("LeftCalf").AsDecimal(5, 2).Nullable();

        Create.Index("Ix_BodyPerimeters_UserId_MeasuredAt").OnTable("dashboard_BodyPerimeters").InSchema("dashboard")
            .OnColumn("UserId").Ascending()
            .OnColumn("MeasuredAt").Ascending();
    }

    public override void Down()
    {
        Delete.Table("dashboard_BodyPerimeters").InSchema("dashboard");
    }
}

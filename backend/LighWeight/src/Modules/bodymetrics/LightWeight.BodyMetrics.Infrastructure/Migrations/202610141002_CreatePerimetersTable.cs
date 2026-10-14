using FluentMigrator;

namespace LightWeight.BodyMetrics.Infrastructure.Migrations;

[Migration(202610141002)]
public class CreatePerimetersTable : Migration
{
    public override void Up()
    {
        // One nullable column per perimeter (cm): a measurement only needs one of them
        Create.Table("bodymetrics_Perimeters").InSchema("bodymetrics")
            .WithColumn("Id").AsGuid().PrimaryKey()
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

        Create.Index("Ix_Perimeters_UserId_MeasuredAt").OnTable("bodymetrics_Perimeters").InSchema("bodymetrics")
            .OnColumn("UserId").Ascending()
            .OnColumn("MeasuredAt").Ascending();
    }

    public override void Down()
    {
        Delete.Table("bodymetrics_Perimeters").InSchema("bodymetrics");
    }
}

using FluentMigrator;

namespace LightWeight.Dashboard.Infrastructure.Migrations;

[Migration(202610151003)]
public class CreateBodySkinFoldsTable : Migration
{
    public override void Up()
    {
        // Projection of the skinfolds of bodymetrics: one nullable column per skinfold (mm). Unlike the source,
        // the summatory is stored because it is what the charts show
        Create.Table("dashboard_BodySkinFolds").InSchema("dashboard")
            .WithColumn("MeasurementId").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("MeasuredAt").AsCustom("timestamp with time zone").NotNullable()
            .WithColumn("Abdominal").AsDecimal(5, 2).Nullable()
            .WithColumn("Suprailiac").AsDecimal(5, 2).Nullable()
            .WithColumn("Tricipital").AsDecimal(5, 2).Nullable()
            .WithColumn("Subscapular").AsDecimal(5, 2).Nullable()
            .WithColumn("Thigh").AsDecimal(5, 2).Nullable()
            .WithColumn("Calf").AsDecimal(5, 2).Nullable()
            .WithColumn("Summatory").AsDecimal(5, 2).Nullable();

        Create.Index("Ix_BodySkinFolds_UserId_MeasuredAt").OnTable("dashboard_BodySkinFolds").InSchema("dashboard")
            .OnColumn("UserId").Ascending()
            .OnColumn("MeasuredAt").Ascending();
    }

    public override void Down()
    {
        Delete.Table("dashboard_BodySkinFolds").InSchema("dashboard");
    }
}

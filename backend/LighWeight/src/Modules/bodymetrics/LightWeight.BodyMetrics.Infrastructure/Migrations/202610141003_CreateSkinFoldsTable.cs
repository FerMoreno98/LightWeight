using FluentMigrator;

namespace LightWeight.BodyMetrics.Infrastructure.Migrations;

[Migration(202610141003)]
public class CreateSkinFoldsTable : Migration
{
    public override void Up()
    {
        // One nullable column per skinfold (mm). The summatory is calculated, not stored
        Create.Table("bodymetrics_SkinFolds").InSchema("bodymetrics")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("MeasuredAt").AsCustom("timestamp with time zone").NotNullable()
            .WithColumn("Abdominal").AsDecimal(5, 2).Nullable()
            .WithColumn("Suprailiac").AsDecimal(5, 2).Nullable()
            .WithColumn("Tricipital").AsDecimal(5, 2).Nullable()
            .WithColumn("Subscapular").AsDecimal(5, 2).Nullable()
            .WithColumn("Thigh").AsDecimal(5, 2).Nullable()
            .WithColumn("Calf").AsDecimal(5, 2).Nullable();

        Create.Index("Ix_SkinFolds_UserId_MeasuredAt").OnTable("bodymetrics_SkinFolds").InSchema("bodymetrics")
            .OnColumn("UserId").Ascending()
            .OnColumn("MeasuredAt").Ascending();
    }

    public override void Down()
    {
        Delete.Table("bodymetrics_SkinFolds").InSchema("bodymetrics");
    }
}

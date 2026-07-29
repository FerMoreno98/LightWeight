using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291200)]
public class CreateMacrocycleTable : Migration
{
    public override void Up()
    {
        Create.Table("training_Macrocycles").InSchema("training")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("StartAt").AsDateTime().NotNullable()
            .WithColumn("EndAt").AsDateTime().Nullable()
            .WithColumn("Stage").AsString(20).NotNullable()
            .WithColumn("Periodization").AsString(20).NotNullable()
            .WithColumn("Comments").AsString().Nullable();
    }

    public override void Down()
    {
        Delete.Table("training_Macrocycles");
    }
}

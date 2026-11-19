using FluentMigrator;

namespace LightWeight.Nutrition.Infrastructure.Migrations;

[Migration(202611191001)]
public class CreateDiaryDaysTable : Migration
{
    public override void Up()
    {
        // Date is a calendar day of the user (Europe/Madrid), without time: a plain date column
        Create.Table("nutrition_DiaryDays").InSchema("nutrition")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("Date").AsDate().NotNullable()
            .WithColumn("Calories").AsDecimal(7, 2).NotNullable()
            .WithColumn("Protein").AsDecimal(6, 2).NotNullable()
            .WithColumn("Carbohydrate").AsDecimal(6, 2).NotNullable()
            .WithColumn("Fat").AsDecimal(6, 2).NotNullable();

        Create.Index("Ux_DiaryDays_UserId_Date").OnTable("nutrition_DiaryDays").InSchema("nutrition")
            .OnColumn("UserId").Ascending()
            .OnColumn("Date").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("nutrition_DiaryDays").InSchema("nutrition");
    }
}

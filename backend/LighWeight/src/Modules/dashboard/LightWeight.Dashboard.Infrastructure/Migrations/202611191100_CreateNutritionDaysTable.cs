using FluentMigrator;

namespace LightWeight.Dashboard.Infrastructure.Migrations;

// Dashboard ranges: 2026101510xx (bodymetrics), 2026102310xx (training), 2026111011xx (userprofile), 2026111911xx (nutrition)
[Migration(202611191100)]
public class CreateNutritionDaysTable : Migration
{
    public override void Up()
    {
        // Projection of the diary days of nutrition, keyed by their DiaryDayId. Date is a calendar day (Europe/Madrid)
        Create.Table("dashboard_NutritionDays").InSchema("dashboard")
            .WithColumn("DiaryDayId").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("Date").AsDate().NotNullable()
            .WithColumn("Calories").AsDecimal(7, 2).NotNullable()
            .WithColumn("Protein").AsDecimal(6, 2).NotNullable()
            .WithColumn("Carbohydrate").AsDecimal(6, 2).NotNullable()
            .WithColumn("Fat").AsDecimal(6, 2).NotNullable();

        Create.Index("Ix_NutritionDays_UserId_Date").OnTable("dashboard_NutritionDays").InSchema("dashboard")
            .OnColumn("UserId").Ascending()
            .OnColumn("Date").Ascending();
    }

    public override void Down()
    {
        Delete.Table("dashboard_NutritionDays").InSchema("dashboard");
    }
}

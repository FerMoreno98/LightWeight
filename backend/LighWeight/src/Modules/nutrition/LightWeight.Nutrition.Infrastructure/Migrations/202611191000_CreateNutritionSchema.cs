using FluentMigrator;

namespace LightWeight.Nutrition.Infrastructure.Migrations;

// Every module shares the same FluentMigrator runner and VersionInfo table, so the versions of
// this module (2026111910xx) must not collide with the ones of the others
[Migration(202611191000)]
public class CreateNutritionSchema : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS nutrition;");
    }

    public override void Down()
    {
        Execute.Sql("DROP SCHEMA IF EXISTS nutrition CASCADE;");
    }
}

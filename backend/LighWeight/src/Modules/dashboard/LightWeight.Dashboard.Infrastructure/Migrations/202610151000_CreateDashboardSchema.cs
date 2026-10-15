using FluentMigrator;

namespace LightWeight.Dashboard.Infrastructure.Migrations;

// Every module shares the same FluentMigrator runner and VersionInfo table, so the versions of
// this module (2026101510xx) must not collide with the ones of the others
[Migration(202610151000)]
public class CreateDashboardSchema : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS dashboard;");
    }

    public override void Down()
    {
        Execute.Sql("DROP SCHEMA IF EXISTS dashboard CASCADE;");
    }
}

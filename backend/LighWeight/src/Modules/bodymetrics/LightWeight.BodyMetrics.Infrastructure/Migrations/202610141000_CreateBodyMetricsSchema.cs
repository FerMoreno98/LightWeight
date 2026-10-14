using FluentMigrator;

namespace LightWeight.BodyMetrics.Infrastructure.Migrations;

// Every module shares the same FluentMigrator runner and VersionInfo table, so the versions of
// this module (2026101410xx) must not collide with the ones of the others
[Migration(202610141000)]
public class CreateBodyMetricsSchema : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS bodymetrics;");
    }

    public override void Down()
    {
        Execute.Sql("DROP SCHEMA IF EXISTS bodymetrics CASCADE;");
    }
}

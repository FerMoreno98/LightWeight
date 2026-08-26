using FluentMigrator;

namespace LightWeight.Auth.Infrastructure.Migrations;

[Migration(202606081150)]
public class CreateAuthSchema : Migration
{
    public override void Up()
    {
       Execute.Sql("CREATE SCHEMA IF NOT EXISTS auth;");
    }
    public override void Down()
    {
        Execute.Sql("DROP SCHEMA IF EXISTS auth CASCADE;");
    }


}
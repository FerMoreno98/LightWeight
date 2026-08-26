using FluentMigrator;

namespace LightWeight.UserProfile.Infrastructure.Migrations;

[Migration(202606111413)]
public class CreateUserProfileSchema : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS userprofile;");
    }
    public override void Down()
    {
        Execute.Sql("DROP SCHEMA IF EXISTS userprofile CASCADE;");
    }


}
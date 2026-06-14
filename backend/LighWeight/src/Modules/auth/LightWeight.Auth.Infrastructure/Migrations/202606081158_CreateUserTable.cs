using FluentMigrator;

namespace LightWeight.Auth.Infrastructure.Migrations;

[Migration(202606081158)]
public class CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("auth_Users").InSchema("auth")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Email").AsString(200).NotNullable().Unique();
    }

    public override void Down()
    {
        Delete.Table("auth_Users");
    }
}
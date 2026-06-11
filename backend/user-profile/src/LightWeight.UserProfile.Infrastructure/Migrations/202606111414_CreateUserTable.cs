using FluentMigrator;

namespace LightWeight.UserProfile.Infrastructure.Migrations;

[Migration(202606111414)]
public class CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("userprofile_Users").InSchema("userprofile")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString(50).NotNullable()
            .WithColumn("Sex").AsString(20).NotNullable()
            .WithColumn("DateOfBirth").AsDateTime().NotNullable()
            .WithColumn("CurrentStage").AsString(20).NotNullable()
            .WithColumn("StageStartedAt").AsDateTime().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("userprofile_Users").InSchema("userprofile");
    }
}
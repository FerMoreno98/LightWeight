using FluentMigrator;

namespace LightWeight.Dashboard.Infrastructure.Migrations;

[Migration(202611101100)]
public class CreateUserProfilesTable : Migration
{
    public override void Up()
    {
        // Projection of the profile of userprofile (only the sex, for the fat percentage), keyed by its UserId
        Create.Table("dashboard_UserProfiles").InSchema("dashboard")
            .WithColumn("UserId").AsGuid().PrimaryKey()
            .WithColumn("Sex").AsString(20).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("dashboard_UserProfiles").InSchema("dashboard");
    }
}

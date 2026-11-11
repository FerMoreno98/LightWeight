using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

/// <summary>
/// Projection of the name of the users (from userprofile), keyed by its UserId, to show the author of a shared
/// or copied program. It starts empty: existing users are filled in by the rebuild ("Recalcular")
/// </summary>
[Migration(202607291219)]
public class CreateUserNamesTable : Migration
{
    public override void Up()
    {
        Create.Table("training_UserNames").InSchema("training")
            .WithColumn("UserId").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString(200).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("training_UserNames").InSchema("training");
    }
}

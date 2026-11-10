using FluentMigrator;

namespace LightWeight.UserProfile.Infrastructure.Migrations;

/// <summary>
/// The training stage moved to Training, but the column stayed as NOT NULL and the entity no longer
/// maps it, so creating a profile failed
/// </summary>
[Migration(202611101000)]
public class DropCurrentStageFromUsers : Migration
{
    public override void Up()
    {
        Delete.Column("CurrentStage").FromTable("userprofile_Users").InSchema("userprofile");
    }

    public override void Down()
    {
        Alter.Table("userprofile_Users").InSchema("userprofile")
            .AddColumn("CurrentStage").AsString(20).Nullable();
    }
}

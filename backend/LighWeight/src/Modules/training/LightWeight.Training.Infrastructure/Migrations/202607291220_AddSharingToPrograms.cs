using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

/// <summary>
/// Programs can be shared with a link and copied by other users. ShareToken is the token of the link (null while
/// not shared, unique while set). CopiedFromProgramId and CopiedFromUserId remember the origin of a copy; they have
/// no FKs because the original program can be deleted
/// </summary>
[Migration(202607291220)]
public class AddSharingToPrograms : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            ALTER TABLE training.""training_Programs""
                ADD COLUMN ""ShareToken"" varchar(12) NULL,
                ADD COLUMN ""CopiedFromProgramId"" uuid NULL,
                ADD COLUMN ""CopiedFromUserId"" uuid NULL;

            CREATE UNIQUE INDEX ""Ux_Program_ShareToken""
                ON training.""training_Programs"" (""ShareToken"")
                WHERE ""ShareToken"" IS NOT NULL;");
    }

    public override void Down()
    {
        Execute.Sql(@"
            DROP INDEX training.""Ux_Program_ShareToken"";

            ALTER TABLE training.""training_Programs""
                DROP COLUMN ""ShareToken"",
                DROP COLUMN ""CopiedFromProgramId"",
                DROP COLUMN ""CopiedFromUserId"";");
    }
}

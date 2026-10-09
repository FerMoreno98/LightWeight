using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

/// <summary>
/// Macrocycles and mesocycles stop having planned dates: they are sequential and are finished explicitly.
/// StartAt/EndAt become StartedAt/FinishedAt (null while active). A user has at most one active macrocycle,
/// a macrocycle at most one active mesocycle, and the week numbers of a mesocycle are unique
/// </summary>
[Migration(202607291216)]
public class MakeCyclesSequential : Migration
{
    public override void Up()
    {
        Rename.Column("StartAt").OnTable("training_Macrocycles").InSchema("training").To("StartedAt");
        Rename.Column("EndAt").OnTable("training_Macrocycles").InSchema("training").To("FinishedAt");
        Rename.Column("StartAt").OnTable("training_Mesocycles").InSchema("training").To("StartedAt");
        Rename.Column("EndAt").OnTable("training_Mesocycles").InSchema("training").To("FinishedAt");

        Execute.Sql(@"
            -- The dates are now set by the system with DateTime.UtcNow, so they are stored with time zone
            -- (like DeletedAt). Existing values are interpreted as UTC
            ALTER TABLE training.""training_Macrocycles""
                ALTER COLUMN ""StartedAt"" TYPE timestamp with time zone USING ""StartedAt"" AT TIME ZONE 'UTC',
                ALTER COLUMN ""FinishedAt"" TYPE timestamp with time zone USING ""FinishedAt"" AT TIME ZONE 'UTC';
            ALTER TABLE training.""training_Mesocycles""
                ALTER COLUMN ""StartedAt"" TYPE timestamp with time zone USING ""StartedAt"" AT TIME ZONE 'UTC',
                ALTER COLUMN ""FinishedAt"" TYPE timestamp with time zone USING ""FinishedAt"" AT TIME ZONE 'UTC',
                ALTER COLUMN ""FinishedAt"" DROP NOT NULL;
            -- A planned end date that has not arrived yet means the cycle is still active
            UPDATE training.""training_Macrocycles"" SET ""FinishedAt"" = NULL WHERE ""FinishedAt"" > now();
            UPDATE training.""training_Mesocycles"" SET ""FinishedAt"" = NULL WHERE ""FinishedAt"" > now();

            -- Keep only the most recent active macrocycle of each user; the older ones are finished now
            UPDATE training.""training_Macrocycles"" m
            SET ""FinishedAt"" = now()
            WHERE m.""FinishedAt"" IS NULL
              AND EXISTS (
                SELECT 1 FROM training.""training_Macrocycles"" newer
                WHERE newer.""UserId"" = m.""UserId""
                  AND newer.""FinishedAt"" IS NULL
                  AND (newer.""StartedAt"", newer.""Id"") > (m.""StartedAt"", m.""Id""));

            -- Same for the active mesocycles of each macrocycle
            UPDATE training.""training_Mesocycles"" m
            SET ""FinishedAt"" = now()
            WHERE m.""FinishedAt"" IS NULL
              AND EXISTS (
                SELECT 1 FROM training.""training_Mesocycles"" newer
                WHERE newer.""MacrocycleId"" = m.""MacrocycleId""
                  AND newer.""FinishedAt"" IS NULL
                  AND (newer.""StartedAt"", newer.""Id"") > (m.""StartedAt"", m.""Id""));

            CREATE UNIQUE INDEX ""Ux_Macrocycle_ActivePerUser""
                ON training.""training_Macrocycles"" (""UserId"") WHERE ""FinishedAt"" IS NULL;
            CREATE UNIQUE INDEX ""Ux_Mesocycle_ActivePerMacrocycle""
                ON training.""training_Mesocycles"" (""MacrocycleId"") WHERE ""FinishedAt"" IS NULL;");

        Create.Index("Ix_Macrocycle_UserId").OnTable("training_Macrocycles").InSchema("training")
            .OnColumn("UserId");
        Create.Index("Ux_Microcycle_WeekNumber").OnTable("training_Microcycles").InSchema("training")
            .OnColumn("MesocycleId").Ascending()
            .OnColumn("WeekNumber").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Index("Ux_Microcycle_WeekNumber").OnTable("training_Microcycles").InSchema("training");
        Delete.Index("Ix_Macrocycle_UserId").OnTable("training_Macrocycles").InSchema("training");
        Execute.Sql(@"
            DROP INDEX training.""Ux_Mesocycle_ActivePerMacrocycle"";
            DROP INDEX training.""Ux_Macrocycle_ActivePerUser"";
            -- Mesocycles required an end date: the active ones end now
            UPDATE training.""training_Mesocycles"" SET ""FinishedAt"" = now() WHERE ""FinishedAt"" IS NULL;
            ALTER TABLE training.""training_Mesocycles""
                ALTER COLUMN ""StartedAt"" TYPE timestamp without time zone USING ""StartedAt"" AT TIME ZONE 'UTC',
                ALTER COLUMN ""FinishedAt"" TYPE timestamp without time zone USING ""FinishedAt"" AT TIME ZONE 'UTC',
                ALTER COLUMN ""FinishedAt"" SET NOT NULL;
            ALTER TABLE training.""training_Macrocycles""
                ALTER COLUMN ""StartedAt"" TYPE timestamp without time zone USING ""StartedAt"" AT TIME ZONE 'UTC',
                ALTER COLUMN ""FinishedAt"" TYPE timestamp without time zone USING ""FinishedAt"" AT TIME ZONE 'UTC';");
        Rename.Column("FinishedAt").OnTable("training_Mesocycles").InSchema("training").To("EndAt");
        Rename.Column("StartedAt").OnTable("training_Mesocycles").InSchema("training").To("StartAt");
        Rename.Column("FinishedAt").OnTable("training_Macrocycles").InSchema("training").To("EndAt");
        Rename.Column("StartedAt").OnTable("training_Macrocycles").InSchema("training").To("StartAt");
    }
}

using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

/// <summary>
/// Weeks become sequential like mesocycles and macrocycles: they get StartedAt/FinishedAt (null while active)
/// and a mesocycle has at most one active week
/// </summary>
[Migration(202607291217)]
public class MakeMicrocyclesSequential : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            ALTER TABLE training.""training_Microcycles""
                ADD COLUMN ""StartedAt"" timestamp with time zone NULL,
                ADD COLUMN ""FinishedAt"" timestamp with time zone NULL;

            -- Existing weeks did not store when they started: use the start of their mesocycle
            UPDATE training.""training_Microcycles"" w
            SET ""StartedAt"" = m.""StartedAt""
            FROM training.""training_Mesocycles"" m
            WHERE m.""Id"" = w.""MesocycleId"";
            ALTER TABLE training.""training_Microcycles"" ALTER COLUMN ""StartedAt"" SET NOT NULL;

            -- The weeks of a finished mesocycle are finished with it
            UPDATE training.""training_Microcycles"" w
            SET ""FinishedAt"" = m.""FinishedAt""
            FROM training.""training_Mesocycles"" m
            WHERE m.""Id"" = w.""MesocycleId"" AND m.""FinishedAt"" IS NOT NULL;

            -- In active mesocycles only the last week stays active; the previous ones are finished now
            UPDATE training.""training_Microcycles"" w
            SET ""FinishedAt"" = now()
            WHERE w.""FinishedAt"" IS NULL
              AND EXISTS (
                SELECT 1 FROM training.""training_Microcycles"" later
                WHERE later.""MesocycleId"" = w.""MesocycleId""
                  AND later.""WeekNumber"" > w.""WeekNumber"");

            CREATE UNIQUE INDEX ""Ux_Microcycle_ActivePerMesocycle""
                ON training.""training_Microcycles"" (""MesocycleId"") WHERE ""FinishedAt"" IS NULL;");
    }

    public override void Down()
    {
        Execute.Sql(@"
            DROP INDEX training.""Ux_Microcycle_ActivePerMesocycle"";
            ALTER TABLE training.""training_Microcycles""
                DROP COLUMN ""FinishedAt"",
                DROP COLUMN ""StartedAt"";");
    }
}

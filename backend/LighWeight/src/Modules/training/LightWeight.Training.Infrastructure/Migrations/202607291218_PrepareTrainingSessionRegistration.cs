using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

/// <summary>
/// Prepares the registration of real sessions: sessions get FinishedAt (null while in progress) and a session of the
/// template is trained at most once per week. The technique of a performed set moves from jsonb to three bool columns,
/// like the planned sets
/// </summary>
[Migration(202607291218)]
public class PrepareTrainingSessionRegistration : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            -- StartAt is set with DateTime.UtcNow, so it is stored with time zone (existing values are UTC)
            ALTER TABLE training.""training_TrainingSessions""
                ALTER COLUMN ""StartAt"" TYPE timestamp with time zone USING ""StartAt"" AT TIME ZONE 'UTC',
                ADD COLUMN ""FinishedAt"" timestamp with time zone NULL;

            CREATE UNIQUE INDEX ""Ux_TrainingSession_TemplateSessionPerWeek""
                ON training.""training_TrainingSessions"" (""MicrocycleId"", ""TemplateSessionId"")
                WHERE ""TemplateSessionId"" IS NOT NULL;

            ALTER TABLE training.""training_Sets""
                ADD COLUMN ""AdvanceTrainingTechniques_IsDropSet"" boolean NOT NULL DEFAULT false,
                ADD COLUMN ""AdvanceTrainingTechniques_IsCluster"" boolean NOT NULL DEFAULT false,
                ADD COLUMN ""AdvanceTrainingTechniques_IsMyoRep"" boolean NOT NULL DEFAULT false;
            UPDATE training.""training_Sets"" SET
                ""AdvanceTrainingTechniques_IsDropSet"" = COALESCE((""AdvanceTrainingTechniques""->>'IsDropSet')::boolean, false),
                ""AdvanceTrainingTechniques_IsCluster"" = COALESCE((""AdvanceTrainingTechniques""->>'IsCluster')::boolean, false),
                ""AdvanceTrainingTechniques_IsMyoRep"" = COALESCE((""AdvanceTrainingTechniques""->>'IsMyoRep')::boolean, false);
            ALTER TABLE training.""training_Sets"" DROP COLUMN ""AdvanceTrainingTechniques"";

            CREATE INDEX ""Ix_Set_ExerciseId"" ON training.""training_Sets"" (""ExerciseId"");");
    }

    public override void Down()
    {
        Execute.Sql(@"
            DROP INDEX training.""Ix_Set_ExerciseId"";
            ALTER TABLE training.""training_Sets"" ADD COLUMN ""AdvanceTrainingTechniques"" jsonb NULL;
            UPDATE training.""training_Sets"" SET ""AdvanceTrainingTechniques"" = jsonb_build_object(
                'IsDropSet', ""AdvanceTrainingTechniques_IsDropSet"",
                'IsCluster', ""AdvanceTrainingTechniques_IsCluster"",
                'IsMyoRep', ""AdvanceTrainingTechniques_IsMyoRep"");
            ALTER TABLE training.""training_Sets""
                ALTER COLUMN ""AdvanceTrainingTechniques"" SET NOT NULL,
                DROP COLUMN ""AdvanceTrainingTechniques_IsDropSet"",
                DROP COLUMN ""AdvanceTrainingTechniques_IsCluster"",
                DROP COLUMN ""AdvanceTrainingTechniques_IsMyoRep"";

            DROP INDEX training.""Ux_TrainingSession_TemplateSessionPerWeek"";
            ALTER TABLE training.""training_TrainingSessions""
                DROP COLUMN ""FinishedAt"",
                ALTER COLUMN ""StartAt"" TYPE timestamp without time zone USING ""StartAt"" AT TIME ZONE 'UTC';");
    }
}

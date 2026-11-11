using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

/// <summary>
/// Sessions and planned sets get an explicit position ("Order") inside their template / session. Until now they were
/// sorted by id, but the version 7 guids created in the same millisecond (duplicating a template, copying a shared
/// program, adding several series at once) do not sort in creation order. It also allows reordering them later.
/// Existing rows keep the order they are shown in today (by id), deleted ones included so no order is reused
/// </summary>
[Migration(202607291221)]
public class AddOrderToTemplateSessionsAndSets : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            ALTER TABLE training.""training_TemplateSessions"" ADD COLUMN ""Order"" integer NULL;
            UPDATE training.""training_TemplateSessions"" s
            SET ""Order"" = ordered.""Position""
            FROM (
                SELECT ""Id"", ROW_NUMBER() OVER (PARTITION BY ""TrainingTemplateId"" ORDER BY ""Id"") AS ""Position""
                FROM training.""training_TemplateSessions""
            ) ordered
            WHERE s.""Id"" = ordered.""Id"";
            ALTER TABLE training.""training_TemplateSessions"" ALTER COLUMN ""Order"" SET NOT NULL;

            ALTER TABLE training.""training_TemplateSets"" ADD COLUMN ""Order"" integer NULL;
            UPDATE training.""training_TemplateSets"" t
            SET ""Order"" = ordered.""Position""
            FROM (
                SELECT ""Id"", ROW_NUMBER() OVER (PARTITION BY ""TemplateSessionId"" ORDER BY ""Id"") AS ""Position""
                FROM training.""training_TemplateSets""
            ) ordered
            WHERE t.""Id"" = ordered.""Id"";
            ALTER TABLE training.""training_TemplateSets"" ALTER COLUMN ""Order"" SET NOT NULL;");
    }

    public override void Down()
    {
        Execute.Sql(@"
            ALTER TABLE training.""training_TemplateSessions"" DROP COLUMN ""Order"";
            ALTER TABLE training.""training_TemplateSets"" DROP COLUMN ""Order"";");
    }
}

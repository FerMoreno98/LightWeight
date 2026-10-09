using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

/// <summary>
/// TemplateSetConfiguration maps RepetitionRange and AdvanceTrainingTechniques as complex properties
/// (one column per field), but 202607291206 created them as jsonb.
/// This migration moves the jsonb values into the columns EF Core expects.
/// (The previous version of this migration could never run: it added a column that already existed
/// and dropped columns that never existed.)
/// </summary>
[Migration(202607291212)]
public class AddAdvanceTrainingTechniquesToTemplateSet : Migration
{
    public override void Up()
    {
        Alter.Table("training_TemplateSets").InSchema("training")
            .AddColumn("RepetitionRange_Min").AsInt32().Nullable()
            .AddColumn("RepetitionRange_Max").AsInt32().Nullable()
            .AddColumn("AdvanceTrainingTechniques_IsDropSet").AsBoolean().Nullable()
            .AddColumn("AdvanceTrainingTechniques_IsCluster").AsBoolean().Nullable()
            .AddColumn("AdvanceTrainingTechniques_IsMyoRep").AsBoolean().Nullable();

        Execute.Sql(@"
            UPDATE training.""training_TemplateSets""
            SET ""RepetitionRange_Min"" = (""RepetitionRange""->>'Min')::int,
                ""RepetitionRange_Max"" = (""RepetitionRange""->>'Max')::int,
                ""AdvanceTrainingTechniques_IsDropSet"" = COALESCE((""AdvanceTrainingTechniques""->>'IsDropSet')::boolean, false),
                ""AdvanceTrainingTechniques_IsCluster"" = COALESCE((""AdvanceTrainingTechniques""->>'IsCluster')::boolean, false),
                ""AdvanceTrainingTechniques_IsMyoRep"" = COALESCE((""AdvanceTrainingTechniques""->>'IsMyoRep')::boolean, false);");

        Alter.Column("RepetitionRange_Min").OnTable("training_TemplateSets").InSchema("training").AsInt32().NotNullable();
        Alter.Column("RepetitionRange_Max").OnTable("training_TemplateSets").InSchema("training").AsInt32().NotNullable();

        Delete.Column("RepetitionRange").Column("AdvanceTrainingTechniques")
            .FromTable("training_TemplateSets").InSchema("training");
    }

    public override void Down()
    {
        Alter.Table("training_TemplateSets").InSchema("training")
            .AddColumn("RepetitionRange").AsCustom("jsonb").Nullable()
            .AddColumn("AdvanceTrainingTechniques").AsCustom("jsonb").Nullable();

        Execute.Sql(@"
            UPDATE training.""training_TemplateSets""
            SET ""RepetitionRange"" = jsonb_build_object('Min', ""RepetitionRange_Min"", 'Max', ""RepetitionRange_Max""),
                ""AdvanceTrainingTechniques"" = jsonb_build_object(
                    'IsDropSet', COALESCE(""AdvanceTrainingTechniques_IsDropSet"", false),
                    'IsCluster', COALESCE(""AdvanceTrainingTechniques_IsCluster"", false),
                    'IsMyoRep', COALESCE(""AdvanceTrainingTechniques_IsMyoRep"", false));");

        Alter.Column("RepetitionRange").OnTable("training_TemplateSets").InSchema("training").AsCustom("jsonb").NotNullable();
        Alter.Column("AdvanceTrainingTechniques").OnTable("training_TemplateSets").InSchema("training").AsCustom("jsonb").NotNullable();

        Delete.Column("RepetitionRange_Min").Column("RepetitionRange_Max")
            .Column("AdvanceTrainingTechniques_IsDropSet")
            .Column("AdvanceTrainingTechniques_IsCluster")
            .Column("AdvanceTrainingTechniques_IsMyoRep")
            .FromTable("training_TemplateSets").InSchema("training");
    }
}

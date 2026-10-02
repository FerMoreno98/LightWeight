using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

/// <summary>
/// Program becomes the aggregate root of the planning side:
/// TrainingTemplate hangs from Program, Mesocycle follows a Program,
/// Microcycle follows a TrainingTemplate and performed sessions/sets keep
/// a reference to the template they come from.
/// Existing templates are preserved by creating one Program per template.
/// Existing mesocycles/microcycles cannot be mapped to a program/template automatically:
/// if any exist, the NOT NULL columns make the migration fail (and roll back) instead of inventing data.
/// </summary>
[Migration(202607291214)]
public class RestructureTrainingAroundProgram : Migration
{
    public override void Up()
    {
        // Program
        Create.Table("training_Programs").InSchema("training")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Periodization").AsString(20).NotNullable()
            .WithColumn("AimMuscleGroups").AsCustom("jsonb").NotNullable();
        Create.Index("Ix_Program_UserId")
            .OnTable("training_Programs").InSchema("training")
            .OnColumn("UserId");

        // TrainingTemplate: one Program per existing template, reusing its Id
        Execute.Sql(@"
            INSERT INTO training.""training_Programs"" (""Id"", ""UserId"", ""Name"", ""Periodization"", ""AimMuscleGroups"")
            SELECT ""Id"", ""UserId"", ""Name"", 'Linear', '[]'::jsonb
            FROM training.""training_TrainingTemplates"";");

        Alter.Table("training_TrainingTemplates").InSchema("training")
            .AddColumn("ProgramId").AsGuid().Nullable()
            .AddColumn("DurationInDays").AsInt32().NotNullable().WithDefaultValue(7)
            .AddColumn("Order").AsInt32().NotNullable().WithDefaultValue(1);
        Execute.Sql(@"UPDATE training.""training_TrainingTemplates"" SET ""ProgramId"" = ""Id"";");
        Alter.Column("ProgramId").OnTable("training_TrainingTemplates").InSchema("training")
            .AsGuid().NotNullable();
        Create.ForeignKey("Fk_TrainingTemplate_Program")
            .FromTable("training_TrainingTemplates").InSchema("training").ForeignColumn("ProgramId")
            .ToTable("training_Programs").InSchema("training").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);
        Create.Index("Ix_TrainingTemplate_ProgramId")
            .OnTable("training_TrainingTemplates").InSchema("training")
            .OnColumn("ProgramId");
        Delete.Column("UserId").Column("Name")
            .FromTable("training_TrainingTemplates").InSchema("training");

        // Macrocycle: periodization moved to Program
        Delete.Column("Periodization")
            .FromTable("training_Macrocycles").InSchema("training");

        // Mesocycle: aim muscle groups moved to Program, now follows a Program
        Delete.Column("AimMuscleGroups")
            .FromTable("training_Mesocycles").InSchema("training");
        Alter.Table("training_Mesocycles").InSchema("training")
            .AddColumn("ProgramId").AsGuid().NotNullable()
                .ForeignKey("Fk_Mesocycle_Program", "training", "training_Programs", "Id")
                .OnDelete(System.Data.Rule.None);
        Create.Index("Ix_Mesocycle_ProgramId")
            .OnTable("training_Mesocycles").InSchema("training")
            .OnColumn("ProgramId");

        // Microcycle: duration/distribution moved to TrainingTemplate, now follows a TrainingTemplate
        Delete.Column("DurationInDays").Column("TrainingDistribution")
            .FromTable("training_Microcycles").InSchema("training");
        Alter.Table("training_Microcycles").InSchema("training")
            .AddColumn("TrainingTemplateId").AsGuid().NotNullable()
                .ForeignKey("Fk_Microcycle_TrainingTemplate", "training", "training_TrainingTemplates", "Id")
                .OnDelete(System.Data.Rule.None)
            .AddColumn("WeekNumber").AsInt32().NotNullable();
        Create.Index("Ix_Microcycle_TrainingTemplateId")
            .OnTable("training_Microcycles").InSchema("training")
            .OnColumn("TrainingTemplateId");

        // Performed sessions/sets keep the template they come from (templates are soft deleted)
        Alter.Table("training_TrainingSessions").InSchema("training")
            .AddColumn("TemplateSessionId").AsGuid().Nullable()
                .ForeignKey("Fk_TrainingSession_TemplateSession", "training", "training_TemplateSessions", "Id")
                .OnDelete(System.Data.Rule.None);
        Create.Index("Ix_TrainingSession_TemplateSessionId")
            .OnTable("training_TrainingSessions").InSchema("training")
            .OnColumn("TemplateSessionId");

        Alter.Table("training_Sets").InSchema("training")
            .AddColumn("TemplateSetId").AsGuid().Nullable()
                .ForeignKey("Fk_Set_TemplateSet", "training", "training_TemplateSets", "Id")
                .OnDelete(System.Data.Rule.None);
        Create.Index("Ix_Set_TemplateSetId")
            .OnTable("training_Sets").InSchema("training")
            .OnColumn("TemplateSetId");

        // TemplateSet: RIR replaced by RPE (RPE = 10 - RIR)
        Alter.Table("training_TemplateSets").InSchema("training")
            .AddColumn("ExpectedRPE").AsCustom("decimal(3,1)").Nullable();
        Execute.Sql(@"
            UPDATE training.""training_TemplateSets""
            SET ""ExpectedRPE"" = LEAST(10, GREATEST(1, 10 - ""ExpectedRIR""));");
        Alter.Column("ExpectedRPE").OnTable("training_TemplateSets").InSchema("training")
            .AsCustom("decimal(3,1)").NotNullable();
        Delete.Column("ExpectedRIR")
            .FromTable("training_TemplateSets").InSchema("training");
    }

    public override void Down()
    {
        // TemplateSet
        Alter.Table("training_TemplateSets").InSchema("training")
            .AddColumn("ExpectedRIR").AsInt32().NotNullable().WithDefaultValue(0);
        Execute.Sql(@"
            UPDATE training.""training_TemplateSets""
            SET ""ExpectedRIR"" = GREATEST(0, 10 - ROUND(""ExpectedRPE""));");
        Delete.Column("ExpectedRPE")
            .FromTable("training_TemplateSets").InSchema("training");

        // Sets / TrainingSessions
        Delete.Index("Ix_Set_TemplateSetId").OnTable("training_Sets").InSchema("training");
        Delete.ForeignKey("Fk_Set_TemplateSet").OnTable("training_Sets").InSchema("training");
        Delete.Column("TemplateSetId").FromTable("training_Sets").InSchema("training");

        Delete.Index("Ix_TrainingSession_TemplateSessionId").OnTable("training_TrainingSessions").InSchema("training");
        Delete.ForeignKey("Fk_TrainingSession_TemplateSession").OnTable("training_TrainingSessions").InSchema("training");
        Delete.Column("TemplateSessionId").FromTable("training_TrainingSessions").InSchema("training");

        // Microcycle
        Delete.Index("Ix_Microcycle_TrainingTemplateId").OnTable("training_Microcycles").InSchema("training");
        Delete.ForeignKey("Fk_Microcycle_TrainingTemplate").OnTable("training_Microcycles").InSchema("training");
        Delete.Column("TrainingTemplateId").Column("WeekNumber")
            .FromTable("training_Microcycles").InSchema("training");
        Alter.Table("training_Microcycles").InSchema("training")
            .AddColumn("DurationInDays").AsInt32().NotNullable().WithDefaultValue(7)
            .AddColumn("TrainingDistribution").AsString(20).NotNullable().WithDefaultValue("Other");

        // Mesocycle
        Delete.Index("Ix_Mesocycle_ProgramId").OnTable("training_Mesocycles").InSchema("training");
        Delete.ForeignKey("Fk_Mesocycle_Program").OnTable("training_Mesocycles").InSchema("training");
        Delete.Column("ProgramId").FromTable("training_Mesocycles").InSchema("training");
        Alter.Table("training_Mesocycles").InSchema("training")
            .AddColumn("AimMuscleGroups").AsCustom("jsonb").NotNullable().WithDefaultValue("[]");

        // Macrocycle
        Alter.Table("training_Macrocycles").InSchema("training")
            .AddColumn("Periodization").AsString(20).NotNullable().WithDefaultValue("Linear");

        // TrainingTemplate: restore owner and name from its Program
        Alter.Table("training_TrainingTemplates").InSchema("training")
            .AddColumn("UserId").AsGuid().Nullable()
            .AddColumn("Name").AsString(200).Nullable();
        Execute.Sql(@"
            UPDATE training.""training_TrainingTemplates"" t
            SET ""UserId"" = p.""UserId"", ""Name"" = p.""Name""
            FROM training.""training_Programs"" p
            WHERE p.""Id"" = t.""ProgramId"";");
        Alter.Column("UserId").OnTable("training_TrainingTemplates").InSchema("training").AsGuid().NotNullable();
        Alter.Column("Name").OnTable("training_TrainingTemplates").InSchema("training").AsString(200).NotNullable();
        Delete.Index("Ix_TrainingTemplate_ProgramId").OnTable("training_TrainingTemplates").InSchema("training");
        Delete.ForeignKey("Fk_TrainingTemplate_Program").OnTable("training_TrainingTemplates").InSchema("training");
        Delete.Column("ProgramId").Column("DurationInDays").Column("Order")
            .FromTable("training_TrainingTemplates").InSchema("training");

        // Program
        Delete.Table("training_Programs").InSchema("training");
    }
}

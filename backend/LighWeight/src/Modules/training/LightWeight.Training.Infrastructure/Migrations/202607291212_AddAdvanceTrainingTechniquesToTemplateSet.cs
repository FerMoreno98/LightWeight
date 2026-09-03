using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291212)]
public class AddAdvanceTrainingTechniquesToTemplateSet : Migration
{
    public override void Down()
    {
            Delete.Column("AdvanceTrainingTechniques")
        .FromTable("training_TemplateSets");
            Alter.Table("training_TemplateSets").InSchema("training")
                .AddColumn("IsCluster")
                .AsBoolean().Nullable();

            Alter.Table("training_TemplateSets").InSchema("training")
                .AddColumn("IsMyoRep")
                .AsBoolean().Nullable();

            Alter.Table("training_TemplateSets").InSchema("training")
                .AddColumn("IsDropSet")
                .AsBoolean().Nullable();
    }

    public override void Up()
    {
        Alter.Table("training_TemplateSets").InSchema("training")
            .AddColumn("AdvanceTrainingTechniques")
            .AsCustom("jsonb").Nullable();
            Delete.Column("IsCluster")
        .FromTable("training_TemplateSets").InSchema("training");
            Delete.Column("IsMyoRep")
        .FromTable("training_TemplateSets").InSchema("training");
            Delete.Column("IsDropSet")
        .FromTable("training_TemplateSets").InSchema("training");
    }
}
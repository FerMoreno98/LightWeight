using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291210)]
public class AddVolumeLandmarkToTrainingTemplate : Migration
{
    public override void Down()
    {
        Delete.Column("VolumeLandmark").FromTable("training_TrainingTemplates");
    }

    public override void Up()
    {
        Alter.Table("training_TrainingTemplates").InSchema("training")
            .AddColumn("VolumeLandmark").AsString().Nullable();
    }
}
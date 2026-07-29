using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291202)]
public class CreateExerciseTable : Migration
{
    public override void Up()
    {
        Create.Table("training_Exercises").InSchema("training")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("IsBilateral").AsBoolean().NotNullable()
            .WithColumn("AimMuscleGroups").AsCustom("jsonb").NotNullable();
    }

    public override void Down()
    {
        Delete.Table("training_Exercises");
    }
}

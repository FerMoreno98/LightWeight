using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

/// <summary>
/// Exercises go from a bilateral flag to a laterality (Bilateral, Unilateral or Both) and split their muscle groups
/// into primary and secondary ones. The existing groups become the primary ones, with no secondary ones
/// </summary>
[Migration(202607291222)]
public class AddLateralityAndSecondaryMusclesToExercises : Migration
{
    public override void Up()
    {
        Execute.Sql(@"
            ALTER TABLE training.""training_Exercises"" ADD COLUMN ""Laterality"" varchar(20) NULL;
            UPDATE training.""training_Exercises""
            SET ""Laterality"" = CASE WHEN ""IsBilateral"" THEN 'Bilateral' ELSE 'Unilateral' END;
            ALTER TABLE training.""training_Exercises"" ALTER COLUMN ""Laterality"" SET NOT NULL;
            ALTER TABLE training.""training_Exercises"" DROP COLUMN ""IsBilateral"";

            ALTER TABLE training.""training_Exercises"" RENAME COLUMN ""AimMuscleGroups"" TO ""PrimaryMuscleGroups"";
            ALTER TABLE training.""training_Exercises"" ADD COLUMN ""SecondaryMuscleGroups"" jsonb NOT NULL DEFAULT '[]';
            ALTER TABLE training.""training_Exercises"" ALTER COLUMN ""SecondaryMuscleGroups"" DROP DEFAULT;");
    }

    public override void Down()
    {
        Execute.Sql(@"
            UPDATE training.""training_Exercises""
            SET ""PrimaryMuscleGroups"" = ""PrimaryMuscleGroups"" || ""SecondaryMuscleGroups"";
            ALTER TABLE training.""training_Exercises"" DROP COLUMN ""SecondaryMuscleGroups"";
            ALTER TABLE training.""training_Exercises"" RENAME COLUMN ""PrimaryMuscleGroups"" TO ""AimMuscleGroups"";

            ALTER TABLE training.""training_Exercises"" ADD COLUMN ""IsBilateral"" boolean NULL;
            UPDATE training.""training_Exercises"" SET ""IsBilateral"" = ""Laterality"" <> 'Unilateral';
            ALTER TABLE training.""training_Exercises"" ALTER COLUMN ""IsBilateral"" SET NOT NULL;
            ALTER TABLE training.""training_Exercises"" DROP COLUMN ""Laterality"";");
    }
}

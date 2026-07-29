using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291201)]
public class CreateMesocycleTable : Migration
{
    public override void Up()
    {
        Create.Table("training_Mesocycles").InSchema("training")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("MacrocycleId").AsGuid().NotNullable()
                .ForeignKey("Fk_Mesocycle_Macrocycle", "training", "training_Macrocycles", "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("AimMuscleGroups").AsCustom("jsonb").NotNullable()
            .WithColumn("MotivationLevel").AsInt32().NotNullable()
            .WithColumn("Injuries").AsString().Nullable()
            .WithColumn("Comments").AsString().Nullable()
            .WithColumn("StartAt").AsDateTime().NotNullable()
            .WithColumn("EndAt").AsDateTime().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("training_Mesocycles");
    }
}

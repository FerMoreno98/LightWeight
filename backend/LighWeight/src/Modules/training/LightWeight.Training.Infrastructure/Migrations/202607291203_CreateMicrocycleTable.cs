using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291203)]
public class CreateMicrocycleTable : Migration
{
    public override void Up()
    {
        Create.Table("training_Microcycles").InSchema("training")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("MesocycleId").AsGuid().NotNullable()
                .ForeignKey("Fk_Microcycle_Mesocycle", "training", "training_Mesocycles", "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("DurationInDays").AsInt32().NotNullable()
            .WithColumn("TrainingDistribution").AsString(20).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("training_Microcycles");
    }
}

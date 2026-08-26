using FluentMigrator;

namespace LightWeight.Training.Infrastructure.Migrations;

[Migration(202607291159)]
public class CreateTrainingSchema : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS training;");
    }
    public override void Down()
    {
        Execute.Sql("DROP SCHEMA IF EXISTS training CASCADE;");
    }


}
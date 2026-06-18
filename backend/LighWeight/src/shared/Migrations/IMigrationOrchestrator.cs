namespace LightWeight.shared.Migrations;
public interface IMigrationOrchestrator
{
    Task RunAllMigrationsAsync();
}
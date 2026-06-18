namespace LightWeight.shared.Migrations;

public class MigrationOrchestrator : IMigrationOrchestrator
{
    private readonly IEnumerable<IModuleMigrationManager> _managers;


    public MigrationOrchestrator(
        IEnumerable<IModuleMigrationManager> managers)
    {
        _managers = managers;
       
    }

    public async Task RunAllMigrationsAsync()
    {
        foreach (var manager in _managers)
        {
            await manager.MigrateAsync();
        }
    }
}
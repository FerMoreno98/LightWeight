using FluentMigrator.Runner;

namespace LightWeight.shared.Migrations;

public class ModuleMigrationManager : IModuleMigrationManager
{
    private readonly IMigrationRunner _runner;
    private readonly string _moduleName;
    // private readonly ILogger<ModuleMigrationManager> _logger;

    public ModuleMigrationManager(
        IMigrationRunner runner,
        string moduleName
       )
    {
        _runner = runner;
        _moduleName = moduleName;
    }

    public string ModuleName => _moduleName;

    public async Task MigrateAsync()
    {
        try
        {
            // _logger.LogInformation("Starting migration for module: {ModuleName}", _moduleName);
            _runner.MigrateUp();
            // _logger.LogInformation("Migration completed for module: {ModuleName}", _moduleName);
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "Migration failed for module: {ModuleName}", _moduleName);
            throw;
        }

        await Task.CompletedTask;
    }
}



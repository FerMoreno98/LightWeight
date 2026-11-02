using FluentMigrator.Runner;
using Microsoft.Extensions.Logging;

namespace LightWeight.shared.Migrations;

public class ModuleMigrationManager : IModuleMigrationManager
{
    private readonly IMigrationRunner _runner;
    private readonly ILogger<ModuleMigrationManager> _logger;
    private readonly string _moduleName;

    public ModuleMigrationManager(
        IMigrationRunner runner,
        ILogger<ModuleMigrationManager> logger,
        string moduleName
       )
    {
        _runner = runner;
        _logger = logger;
        _moduleName = moduleName;
    }

    public string ModuleName => _moduleName;

    public async Task MigrateAsync()
    {
        try
        {
            _logger.LogInformation("Starting migration for module: {ModuleName}", _moduleName);
            _runner.MigrateUp();
            _logger.LogInformation("Migration completed for module: {ModuleName}", _moduleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed for module: {ModuleName}", _moduleName);
            throw;
        }

        await Task.CompletedTask;
    }
}



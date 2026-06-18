namespace LightWeight.shared.Migrations;

public interface IModuleMigrationManager
{
    string ModuleName { get; }
    Task MigrateAsync();
}


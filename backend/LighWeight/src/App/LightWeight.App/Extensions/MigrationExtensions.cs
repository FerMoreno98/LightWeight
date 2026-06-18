using LightWeight.shared.Migrations;

namespace LightWeight.App.Extensions;

public static class MigrationExtensions
{
    public static async Task RunModuleMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var orchestrator = scope.ServiceProvider
            .GetRequiredService<IMigrationOrchestrator>();
            
        await orchestrator.RunAllMigrationsAsync();
    }
}
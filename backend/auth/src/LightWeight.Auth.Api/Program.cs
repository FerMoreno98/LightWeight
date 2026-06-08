using FluentMigrator.Runner;                 
using LightWeight.Auth.Infrastructure;  

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure();


var app = builder.Build();



app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}

app.Run();



using FluentMigrator.Runner;
using LightWeight.Auth.Api.Middlewares;
using LightWeight.Auth.API.Middlewares;
using LightWeight.Auth.Infrastructure;  

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<AuthExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddInfrastructure();


var app = builder.Build();



app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}
app.UseExceptionHandler();
app.Run();



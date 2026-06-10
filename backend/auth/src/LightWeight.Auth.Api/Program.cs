using FluentMigrator.Runner;
using LightWeight.Auth.Api.Middlewares;
using LightWeight.Auth.API.Middlewares;
using LightWeight.Auth.Infrastructure;
using LightWeight.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<AuthExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers(); 
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);



var app = builder.Build();
app.UseAuthentication();                    
app.UseAuthorization(); 


app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}
app.UseExceptionHandler();
app.MapControllers();
app.Run();



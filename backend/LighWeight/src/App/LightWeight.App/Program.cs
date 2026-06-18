
using System.Text;
using FluentMigrator.Runner;
using LightWeight.App.Extensions;
using LightWeight.Auth.Infrastructure;
using LightWeight.shared.Migrations;
using LightWeight.UserProfile.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthModule(builder.Configuration)
    .AddUserProfileModule(builder.Configuration);

// Registrar el orquestador
builder.Services.AddScoped<IMigrationOrchestrator, MigrationOrchestrator>();

// Configurar autorización SIN política por defecto
builder.Services.AddAuthorization(options =>
{
    // No establecemos FallbackPolicy, así que por defecto NO requiere autenticación
    // Solo agregamos políticas nombradas si las necesitamos
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ValidateIssuer   = true,
            ValidIssuer      = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience    = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true
        };
    });
var app = builder.Build();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
// Ejecutar migraciones de forma ordenada
await app.RunModuleMigrationsAsync();

// 2. Mapear los endpoints de cada módulo
app.MapAuthEndpoints();
app.MapUserProfileEndpoints();




app.Run();



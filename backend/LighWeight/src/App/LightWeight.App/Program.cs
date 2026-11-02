
using System.Security.Claims;
using System.Text;
using FluentMigrator.Runner;
using LightWeight.App.Exceptions;
using LightWeight.App.Extensions;
using LightWeight.Auth.Infrastructure;
using LightWeight.BodyMetrics.Infrastructure;
using LightWeight.Dashboard.Infrastructure;
using LightWeight.shared.Migrations;
using LightWeight.UserProfile.Infrastructure;
using LightWeight.Training.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using LightWeight.Training.Api;
using LightWeight.BodyMetrics.Api;
using LightWeight.Dashboard.Api;
using Serilog;
var builder = WebApplication.CreateBuilder(args);

// Destinos y niveles en appsettings.json; los módulos solo usan ILogger
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? throw new InvalidOperationException("Cors:AllowedOrigins not configured");
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services
    .AddAuthModule(builder.Configuration)
    .AddUserProfileModule(builder.Configuration)
    .AddTrainingModule(builder.Configuration)
    .AddBodyMetricsModule(builder.Configuration)
    .AddDashboardModule(builder.Configuration);

// Registrar el orquestador
builder.Services.AddScoped<IMigrationOrchestrator, MigrationOrchestrator>();

// Toda excepción que llegue al host se responde como ProblemDetails
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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
// Antes que el manejador de excepciones, para registrar el estado final de cada respuesta
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms for user {UserId}";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        diagnosticContext.Set("UserId", httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
});
// Captura lo que lance cualquier middleware posterior
app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();
// Ejecutar migraciones de forma ordenada
await app.RunModuleMigrationsAsync();
await app.SeedTrainingDataAsync();

// 2. Mapear los endpoints de cada módulo
app.MapAuthEndpoints();
app.MapUserProfileEndpoints();
app.MapTrainingEndpoints();
app.MapBodyMetricsEndpoints();
app.MapDashboardEndpoints();




app.Run();



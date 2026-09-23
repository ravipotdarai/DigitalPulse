using DigitalPulse.Api.Endpoints;
using DigitalPulse.Api.Middleware;
using DigitalPulse.Application;
using DigitalPulse.Infrastructure;
using DigitalPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    var ok = await db.Database.CanConnectAsync(cancellationToken);
    return ok ? Results.Ok(new { status = "ok" }) : Results.Json(new { status = "degraded" }, statusCode: 503);
});
app.MapOpenApi();
app.MapAuthEndpoints();
app.MapTenantEndpoints();
app.MapBusinessEndpoints();
app.MapIdentityEndpoints();
app.MapConnectionEndpoints();
app.MapScanEndpoints();
app.MapWebsiteEndpoints();
app.MapBillingAndOnboardingEndpoints();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
    {
        if (db.Database.GetMigrations().Any())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        await IdentitySchemaUpgrader.EnsureAsync(db);
        await ConnectionsSchemaUpgrader.EnsureAsync(db);
        await ScansSchemaUpgrader.EnsureAsync(db);
        await WebsiteSchemaUpgrader.EnsureAsync(db);
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }
    await DatabaseSeeder.SeedAsync(db);
}

await app.RunAsync();

public partial class Program;

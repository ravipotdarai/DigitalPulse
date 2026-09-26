using System.Security.Claims;
using System.Threading.RateLimiting;
using DigitalPulse.Api.Endpoints;
using DigitalPulse.Api.Middleware;
using DigitalPulse.Application;
using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Operations;
using DigitalPulse.Infrastructure;
using DigitalPulse.Infrastructure.Configuration;
using DigitalPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

LocalEnvFile.Load();
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
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/ready", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter("health");
        }

        var key = context.User.FindFirstValue("tenant_id")
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anon";
        var limit = context.RequestServices.GetRequiredService<IOperationsEnvironment>().RateLimitPerMinute;
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit < 1 ? OperationsPolicy.RateLimitPerMinute : limit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ResponseTimingMiddleware>();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
if (!app.Configuration.GetValue<bool>("Testing:UseInMemory"))
{
    app.UseRateLimiter();
}
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    var ok = await db.Database.CanConnectAsync(cancellationToken);
    return ok ? Results.Ok(new { status = "ok" }) : Results.Json(new { status = "degraded" }, statusCode: 503);
});
app.MapGet("/ready", async (AppDbContext db, IOperationsEnvironment env, CancellationToken cancellationToken) =>
{
    var sql = await db.Database.CanConnectAsync(cancellationToken);
    if (!sql)
    {
        return Results.Json(new { status = "degraded", environment = env.EnvironmentName }, statusCode: 503);
    }

    return Results.Ok(new
    {
        status = "ready",
        environment = env.EnvironmentName,
        role = env.HostRole,
        holds = new[]
        {
            env.KeyVaultConfigured ? null : OperationsPolicy.KeyVaultHold,
            env.AppInsightsConfigured ? null : OperationsPolicy.InsightsHold,
            env.RedisConfigured ? null : OperationsPolicy.RedisHold,
            env.AzureBackupConfigured ? null : OperationsPolicy.AzureBackupHold
        }.Where(h => h is not null)
    });
});
app.MapOpenApi();
app.MapAuthEndpoints();
app.MapTenantEndpoints();
app.MapBusinessEndpoints();
app.MapIdentityEndpoints();
app.MapConnectionEndpoints();
app.MapScanEndpoints();
app.MapWebsiteEndpoints();
app.MapSocialEndpoints();
app.MapDirectoryEndpoints();
app.MapProjectEndpoints();
app.MapContentEndpoints();
app.MapAiEndpoints();
app.MapActionEndpoints();
app.MapWhatsAppEndpoints();
app.MapMonitoringEndpoints();
app.MapBillingEndpoints();
app.MapAgencyEndpoints();
app.MapOperationsEndpoints();
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
        await SocialSchemaUpgrader.EnsureAsync(db);
        await DirectorySchemaUpgrader.EnsureAsync(db);
        await ProjectSchemaUpgrader.EnsureAsync(db);
        await ContentHubSchemaUpgrader.EnsureAsync(db);
        await AiSchemaUpgrader.EnsureAsync(db);
        await ActionSchemaUpgrader.EnsureAsync(db);
        await WhatsAppSchemaUpgrader.EnsureAsync(db);
        await MonitoringSchemaUpgrader.EnsureAsync(db);
        await BillingSchemaUpgrader.EnsureAsync(db);
        await AgencySchemaUpgrader.EnsureAsync(db);
        await OperationsSchemaUpgrader.EnsureAsync(db);
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }
    await DatabaseSeeder.SeedAsync(db);
}

await app.RunAsync();

public partial class Program;

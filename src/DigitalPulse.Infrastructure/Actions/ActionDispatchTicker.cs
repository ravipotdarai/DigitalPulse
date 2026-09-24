using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Features.Actions;
using DigitalPulse.Domain.Actions;
using DigitalPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DigitalPulse.Infrastructure.Actions;

public sealed class ActionDispatchTicker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ActionDispatchTicker> _logger;

    public ActionDispatchTicker(IServiceScopeFactory scopes, ILogger<ActionDispatchTicker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Action dispatch tick failed. Live provider writes were not invented.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
        await using var scan = new AppDbContext(options, tenantContext: null);
        var now = DateTimeOffset.UtcNow;
        var due = await scan.WorkActions
            .Where(a =>
                a.Status == ActionStatus.Queued ||
                (a.Status == ActionStatus.Failed && a.NextRetryAtUtc != null && a.NextRetryAtUtc <= now))
            .OrderBy(a => a.CreatedAtUtc)
            .Take(20)
            .Select(a => new { a.Id, a.TenantId, a.BusinessId })
            .ToListAsync(cancellationToken);

        foreach (var item in due)
        {
            await using var inner = _scopes.CreateAsyncScope();
            using (AmbientTenant.Use(item.TenantId))
            {
                var execute = inner.ServiceProvider.GetRequiredService<ExecuteActionHandler>();
                await execute.Handle(item.BusinessId, item.Id, cancellationToken);
            }
        }
    }
}

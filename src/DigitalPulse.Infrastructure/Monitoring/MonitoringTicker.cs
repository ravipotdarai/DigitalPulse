using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Monitoring;
using DigitalPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DigitalPulse.Infrastructure.Monitoring;

public sealed class MonitoringTicker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<MonitoringTicker> _logger;

    public MonitoringTicker(IServiceScopeFactory scopes, ILogger<MonitoringTicker> logger)
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
                _logger.LogWarning(ex, "Scheduled monitoring tick failed. Live provider metrics were not invented.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
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
        var fetcher = scope.ServiceProvider.GetRequiredService<IWebsiteFetcher>();
        var adapters = scope.ServiceProvider.GetRequiredService<IPlatformAdapterCatalog>();
        await using var db = new AppDbContext(options, tenantContext: null);

        var businesses = await db.Businesses.AsNoTracking()
            .Select(b => new { b.Id, b.TenantId })
            .ToListAsync(cancellationToken);
        if (businesses.Count == 0)
        {
            return;
        }

        var subscribed = await db.Subscriptions.AsNoTracking()
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Select(s => s.TenantId)
            .ToListAsync(cancellationToken);

        foreach (var business in businesses)
        {
            if (!subscribed.Contains(business.TenantId))
            {
                continue;
            }

            await Application.Features.Monitoring.MonitoringEngine.RunAsync(
                db,
                fetcher,
                adapters,
                business.TenantId,
                business.Id,
                MonitoringTrigger.Scheduled,
                cancellationToken);
        }
    }
}

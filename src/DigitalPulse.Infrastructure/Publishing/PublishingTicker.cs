using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Features.Content;
using DigitalPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DigitalPulse.Infrastructure.Publishing;

public sealed class PublishingTicker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<PublishingTicker> _logger;

    public PublishingTicker(IServiceScopeFactory scopes, ILogger<PublishingTicker> logger)
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
                _logger.LogWarning(ex, "Scheduled publishing tick failed. Provider posts were not invented.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
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
        var catalog = scope.ServiceProvider.GetService<IPlatformAdapterCatalog>();
        await using var db = new AppDbContext(options, tenantContext: null);
        var released = await ContentPublishingJobs.ReleaseDueAsync(db, null, ignoreFilters: true, cancellationToken);
        var retried = await ContentPublishingJobs.RetryHeldAsync(db, catalog, null, ignoreFilters: true, cancellationToken);
        var verified = await ContentPublishingJobs.VerifyConfirmedAsync(db, null, ignoreFilters: true, cancellationToken);
        if (released + retried + verified == 0)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Publishing tick released {Released} due articles, retried {Retried} holds, verified {Verified} confirmed rows.",
            released,
            retried,
            verified);
    }
}

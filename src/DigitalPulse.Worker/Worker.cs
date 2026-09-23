namespace DigitalPulse.Worker;

public sealed class PulseWorker : BackgroundService
{
    private readonly ILogger<PulseWorker> _logger;

    public PulseWorker(ILogger<PulseWorker> logger) => _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("DigitalPulse worker idle at {Time}. Onboarding slice has no background jobs.", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}

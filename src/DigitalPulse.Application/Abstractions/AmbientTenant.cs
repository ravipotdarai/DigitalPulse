namespace DigitalPulse.Application.Abstractions;

public static class AmbientTenant
{
    private static readonly AsyncLocal<Guid?> CurrentValue = new();

    public static Guid? Current => CurrentValue.Value;

    public static IDisposable Use(Guid tenantId)
    {
        var previous = CurrentValue.Value;
        CurrentValue.Value = tenantId;
        return new Reset(previous);
    }

    private sealed class Reset(Guid? previous) : IDisposable
    {
        public void Dispose() => CurrentValue.Value = previous;
    }
}

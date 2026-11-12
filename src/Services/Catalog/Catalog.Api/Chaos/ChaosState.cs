using Catalog.Api.Contracts;

namespace Catalog.Api.Chaos;

/// <summary>In-memory fault-injection settings. They reset to "no faults" when the service restarts.</summary>
public sealed class ChaosState
{
    private ChaosSettings _settings = new();

    public ChaosSettings Current => Volatile.Read(ref _settings);

    public void Update(ChaosSettings settings) => Volatile.Write(ref _settings, settings);
}

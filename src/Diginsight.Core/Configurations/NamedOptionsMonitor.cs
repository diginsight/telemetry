using Microsoft.Extensions.Options;

namespace Diginsight;

// Add this class at the bottom of your HostingExtensions.cs file
public sealed class NamedOptionsMonitor<T> : IOptionsMonitor<T>
{
    IOptionsMonitor<T> innerOptionsMonitor = default!;
    public T CurrentValue { get; set; }

    public NamedOptionsMonitor(IOptionsMonitor<T> innerOptionsMonitor, string? name = null)
    {
        this.innerOptionsMonitor = innerOptionsMonitor ?? throw new ArgumentNullException(nameof(innerOptionsMonitor));
        this.CurrentValue = innerOptionsMonitor.Get(name);
    }
    public T Get(string? name)
    {
        return innerOptionsMonitor.Get(name);
    }

    public IDisposable? OnChange(Action<T, string?> listener)
    {
        return innerOptionsMonitor.OnChange(listener);
    }
}

public sealed class NamedOptions<T> : IOptions<T> where T : class
{
    public T Value { get; }

    public NamedOptions(IOptionsMonitor<T> optionsMonitor, string name)
    {
        Value = optionsMonitor.Get(name);
    }
}


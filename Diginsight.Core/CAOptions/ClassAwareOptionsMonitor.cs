using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Diginsight.CAOptions;

public sealed class ClassAwareOptionsMonitor<TOptions> : IClassAwareOptionsMonitor<TOptions>, IDisposable
    where TOptions : class
{
    private readonly IClassAwareOptionsFactory<TOptions> factory;
    private readonly IClassAwareOptionsCache<TOptions> cache;
    private readonly ICollection<IDisposable> changeRegistrations = new List<IDisposable>();

    private event Action<IReadOnlyDictionary<Type, TOptions>, string>? Change;

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    TOptions IOptionsMonitor<TOptions>.CurrentValue => Get(Options.DefaultName, ClassAwareOptions.NoType);
#endif

    public ClassAwareOptionsMonitor(
        IClassAwareOptionsFactory<TOptions> factory,
        IClassAwareOptionsCache<TOptions> cache,
        IEnumerable<IOptionsChangeTokenSource<TOptions>> sources
    )
    {
        this.factory = factory;
        this.cache = cache;
        foreach (IOptionsChangeTokenSource<TOptions> source in sources)
        {
            IDisposable changeRegistration = ChangeToken.OnChange(source.GetChangeToken, InvokeChanged, source.Name);
            changeRegistrations.Add(changeRegistration);
        }
    }

    private void InvokeChanged(string? name)
    {
        name ??= Options.DefaultName;
        IEnumerable<Type> classes = cache.TryRemove(name);
        Change?.Invoke(classes.ToDictionary(static c => c, c => Get(name, c)), name);
    }

    public TOptions Get(string name, Type @class)
    {
        return cache.GetOrAdd(name, @class, static (n, c, f) => f.Create(n, c), factory);
    }

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    TOptions IOptionsMonitor<TOptions>.Get(string? name) => Get(name ?? Options.DefaultName, ClassAwareOptions.NoType);
#endif

    public IDisposable OnChange(Action<IReadOnlyDictionary<Type, TOptions>, string> listener)
    {
        ChangeTrackerDisposable changeTrackerDisposable = new(this, listener);
        Change += changeTrackerDisposable.OnChange;
        return changeTrackerDisposable;
    }

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    [Obsolete("Use the other overload instead", true)]
    public IDisposable? OnChange(Action<TOptions, string?> listener)
    {
        throw new NotSupportedException("Use the other overload instead");
    }
#endif

    public void Dispose()
    {
        foreach (IDisposable registration in changeRegistrations)
        {
            registration.Dispose();
        }
        changeRegistrations.Clear();
    }

    private sealed class ChangeTrackerDisposable : IDisposable
    {
        private readonly ClassAwareOptionsMonitor<TOptions> owner;
        private readonly Action<IReadOnlyDictionary<Type, TOptions>, string> listener;

        public ChangeTrackerDisposable(ClassAwareOptionsMonitor<TOptions> owner, Action<IReadOnlyDictionary<Type, TOptions>, string> listener)
        {
            this.owner = owner;
            this.listener = listener;
        }

        public void OnChange(IReadOnlyDictionary<Type, TOptions> options, string name)
        {
            listener(options, name);
        }

        public void Dispose()
        {
            owner.Change -= OnChange;
        }
    }
}

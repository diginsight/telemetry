using Microsoft.Extensions.Primitives;
#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
using Microsoft.Extensions.Options;
#endif

namespace Diginsight.CAOptions;

public sealed class ClassAwareOptionsMonitor<TOptions> : IClassAwareOptionsMonitor<TOptions>, IDisposable
    where TOptions : class
{
    private readonly IClassAwareOptionsFactory<TOptions> factory;
    private readonly IClassAwareOptionsCache<TOptions> cache;
    private readonly ICollection<IDisposable> changeRegistrations = new List<IDisposable>();

    private event Action<TOptions, string, Type>? Change;

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    TOptions IOptionsMonitor<TOptions>.CurrentValue => Get(Options.DefaultName, ClassAwareOptions.NoType);
#endif

    public ClassAwareOptionsMonitor(
        IClassAwareOptionsFactory<TOptions> factory,
        IClassAwareOptionsCache<TOptions> cache,
        IEnumerable<IClassAwareOptionsChangeTokenSource<TOptions>> sources
    )
    {
        this.factory = factory;
        this.cache = cache;

        foreach (IClassAwareOptionsChangeTokenSource<TOptions> source in sources)
        {
            IDisposable changeRegistration = ChangeToken.OnChange(source.GetChangeToken, InvokeChanged, source.Name);
            changeRegistrations.Add(changeRegistration);
        }
    }

    private void InvokeChanged(string? name)
    {
        IEnumerable<(string Name, IEnumerable<Type> Classes)> namesWithClasses;
        if (name is null)
        {
            namesWithClasses = cache.Clear();
        }
        else
        {
            IEnumerable<Type> classes = cache.TryRemove(name);
            namesWithClasses = [ (name, classes) ];
        }

        foreach ((string removedName, IEnumerable<Type> removedClasses) in namesWithClasses)
        {
            foreach (Type @class in removedClasses)
            {
                Change?.Invoke(Get(removedName, @class), removedName, @class);
            }
        }
    }

    public TOptions Get(string name, Type @class)
    {
        return cache.GetOrAdd(name, @class, static (n, c, f) => f.Create(n, c), factory);
    }

#if !(NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
    TOptions IOptionsMonitor<TOptions>.Get(string? name) => Get(name ?? Options.DefaultName, ClassAwareOptions.NoType);
#endif

    public IDisposable OnChange(Action<TOptions, string, Type> listener)
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
        private readonly Action<TOptions, string, Type> listener;

        public ChangeTrackerDisposable(ClassAwareOptionsMonitor<TOptions> owner, Action<TOptions, string, Type> listener)
        {
            this.owner = owner;
            this.listener = listener;
        }

        public void OnChange(TOptions options, string name, Type @class)
        {
            listener(options, name, @class);
        }

        public void Dispose()
        {
            owner.Change -= OnChange;
        }
    }
}

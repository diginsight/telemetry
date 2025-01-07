using Microsoft.Extensions.Primitives;
#if !(NET || NETSTANDARD2_1_OR_GREATER)
using Microsoft.Extensions.Options;
#endif

namespace Diginsight.Options;

/// <summary>
/// Default implementation of the <see cref="IClassAwareOptionsMonitor{TOptions}" /> interface.
/// </summary>
/// <typeparam name="TOptions">The type of options to cache.</typeparam>
public sealed class ClassAwareOptionsMonitor<TOptions> : IClassAwareOptionsMonitor<TOptions>, IDisposable
    where TOptions : class
{
    private readonly IClassAwareOptionsFactory<TOptions> factory;
    private readonly IClassAwareOptionsCache<TOptions> cache;
    private readonly ICollection<IDisposable> changeRegistrations = new List<IDisposable>();

    private event Action<TOptions, string, Type>? Change;

#if !(NET || NETSTANDARD2_1_OR_GREATER)
    TOptions IOptionsMonitor<TOptions>.CurrentValue => Get(null, null);
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassAwareOptionsMonitor{TOptions}" /> class.
    /// </summary>
    /// <param name="factory">The factory to create options instances.</param>
    /// <param name="cache">The cache to store options instances.</param>
    /// <param name="sources">The sources for change tokens to monitor options changes.</param>
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

    /// <inheritdoc />
    public TOptions Get(string? name, Type? @class)
    {
        return cache.GetOrAdd(
            name ?? Microsoft.Extensions.Options.Options.DefaultName,
            @class ?? ClassAwareOptions.NoClass,
            static (n, c, f) => f.Create(n, c),
            factory
        );
    }

#if !(NET || NETSTANDARD2_1_OR_GREATER)
    TOptions IOptionsMonitor<TOptions>.Get(string? name) => Get(name, null);
#endif

    /// <inheritdoc />
    public IDisposable OnChange(Action<TOptions, string, Type> listener)
    {
        ChangeTrackerDisposable changeTrackerDisposable = new (this, listener);
        Change += changeTrackerDisposable.OnChange;
        return changeTrackerDisposable;
    }

#if !(NET || NETSTANDARD2_1_OR_GREATER)
    public IDisposable OnChange(Action<TOptions, string?> listener)
    {
        return OnChange((options, name, _) => listener(options, name));
    }
#endif

    /// <inheritdoc />
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

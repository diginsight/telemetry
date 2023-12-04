using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Diginsight.CAOptions;

public sealed class ClassAwareOptionsMonitor<TOptions, TClass> : IClassAwareOptionsMonitor<TOptions, TClass>, IDisposable
    where TOptions : class
{
    private readonly IClassAwareOptionsFactory<TOptions> factory;
    private readonly IClassAwareOptionsCache<TOptions> cache;
    private readonly ICollection<IDisposable> registrations = new List<IDisposable>();

    public TOptions CurrentValue => Get(Options.DefaultName);

    private event Action<TOptions, string>? Change;

    public ClassAwareOptionsMonitor(
        IClassAwareOptionsFactory<TOptions> factory,
        IClassAwareOptionsCache<TOptions> cache,
        IEnumerable<IClassAwareOptionsChangeTokenSource<TOptions>> sources
    )
    {
        this.factory = factory;
        this.cache = cache;
        foreach (IClassAwareOptionsChangeTokenSource<TOptions> source in sources.Where(static x => x.Class == typeof(TClass)))
        {
            IDisposable item = ChangeToken.OnChange(source.GetChangeToken, InvokeChanged, source.Name);
            registrations.Add(item);
        }
    }

    private void InvokeChanged(string? name)
    {
        name ??= Options.DefaultName;
        cache.TryRemove(name, typeof(TClass));
        Change?.Invoke(Get(name), name);
    }

    public TOptions Get(string? name)
    {
        name ??= Options.DefaultName;
        return cache.GetOrAdd(name, typeof(TClass), () => factory.Create(name, typeof(TClass)));
    }

    public IDisposable OnChange(Action<TOptions, string> listener)
    {
        ChangeTrackerDisposable changeTrackerDisposable = new (this, listener);
        Change += changeTrackerDisposable.OnChange;
        return changeTrackerDisposable;
    }

    public void Dispose()
    {
        foreach (IDisposable registration in registrations)
        {
            registration.Dispose();
        }
        registrations.Clear();
    }

    private sealed class ChangeTrackerDisposable : IDisposable
    {
        private readonly ClassAwareOptionsMonitor<TOptions, TClass> owner;
        private readonly Action<TOptions, string> listener;

        public ChangeTrackerDisposable(ClassAwareOptionsMonitor<TOptions, TClass> owner, Action<TOptions, string> listener)
        {
            this.owner = owner;
            this.listener = listener;
        }

        public void OnChange(TOptions options, string name)
        {
            listener(options, name);
        }

        public void Dispose()
        {
            owner.Change -= OnChange;
        }
    }
}

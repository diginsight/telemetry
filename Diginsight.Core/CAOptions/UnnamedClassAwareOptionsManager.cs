using Microsoft.Extensions.Options;

namespace Diginsight.CAOptions;

internal sealed class UnnamedClassAwareOptionsManager<TOptions, TClass> : IClassAwareOptions<TOptions, TClass>
    where TOptions : class
{
    private readonly IClassAwareOptionsFactory<TOptions> factory;
    private readonly object lockObj = new ();

    private TOptions? value;

    public TOptions Value
    {
        get
        {
            if (value is { } result)
            {
                return result;
            }

            lock (lockObj)
            {
                return value ??= factory.Create(Options.DefaultName, typeof(TClass));
            }
        }
    }

    public UnnamedClassAwareOptionsManager(IClassAwareOptionsFactory<TOptions> factory)
    {
        this.factory = factory;
    }
}

using System.Diagnostics;

namespace Diginsight;

public abstract class DiginsightPropagator : DistributedContextPropagator
{
    private readonly IDiginsightDistributedContextOptions distributedContextOptions;

    protected DistributedContextPropagator Decoratee { get; }

    public override IReadOnlyCollection<string> Fields => Decoratee.Fields;

    protected DiginsightPropagator(
        DistributedContextPropagator decoratee,
        IDiginsightDistributedContextOptions distributedContextOptions
    )
    {
        Decoratee = decoratee;
        this.distributedContextOptions = distributedContextOptions;
    }

    public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter)
    {
        if (carrier is not null && getter is not null)
        {
            SetCurrentNonBaggage(
                distributedContextOptions.NonBaggageKeys.ToDictionary(
                    static x => x,
                    x =>
                    {
                        getter(carrier, x, out var fv, out var fvs);
                        return fvs ?? (fv is null ? [ ] : [ fv ]);
                    }
                )
            );
        }

        return Decoratee.ExtractBaggage(carrier, getter);
    }

    public override void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState)
    {
        Decoratee.ExtractTraceIdAndState(carrier, getter, out traceId, out traceState);
    }

    protected abstract void SetCurrentNonBaggage(IEnumerable<KeyValuePair<string, IEnumerable<string>>> nonBaggage);

    public override void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter)
    {
        Decoratee.Inject(activity, carrier, setter);

        if (carrier is null || setter is null || GetCurrentNonBaggage() is not { } nonBaggage)
            return;

        foreach ((string key, IEnumerable<string> values) in nonBaggage)
        {
#if NET || NETSTANDARD2_1_OR_GREATER
            setter(carrier, key, string.Join(',', values));
#else
            setter(carrier, key, string.Join(",", values));
#endif
        }
    }

    protected abstract IEnumerable<KeyValuePair<string, IEnumerable<string>>>? GetCurrentNonBaggage();
}

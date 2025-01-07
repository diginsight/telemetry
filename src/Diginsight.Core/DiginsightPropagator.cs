using System.Diagnostics;

namespace Diginsight;

/// <summary>
/// Abstract class for decorating a <see cref="DistributedContextPropagator" /> with logic
/// to transport distributed context information in non-baggage item.
/// </summary>
/// <remarks>
/// Implementors must override <see cref="SetCurrentNonBaggage" /> and <see cref="GetCurrentNonBaggage" />
/// to store and retrieve non-baggage items in the current context (for example, in a Web API, the HTTP context).
/// </remarks>
public abstract class DiginsightPropagator : DistributedContextPropagator
{
    private readonly IDiginsightDistributedContextOptions distributedContextOptions;

    /// <summary>
    /// Gets the decoratee propagator.
    /// </summary>
    protected DistributedContextPropagator Decoratee { get; }

    /// <inheritdoc />
    public override IReadOnlyCollection<string> Fields { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiginsightPropagator" /> class.
    /// </summary>
    /// <param name="decoratee">The decoratee propagator.</param>
    /// <param name="distributedContextOptions">The distributed context options.</param>
    protected DiginsightPropagator(
        DistributedContextPropagator decoratee,
        IDiginsightDistributedContextOptions distributedContextOptions
    )
    {
        this.distributedContextOptions = distributedContextOptions;

        Decoratee = decoratee;
        Fields = [ .. decoratee.Fields, .. distributedContextOptions.NonBaggageKeys ];
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState)
    {
        Decoratee.ExtractTraceIdAndState(carrier, getter, out traceId, out traceState);
    }

    /// <summary>
    /// Sets the current non-baggage items.
    /// </summary>
    /// <param name="nonBaggage">The non-baggage items extracted from the carrier.</param>
    protected abstract void SetCurrentNonBaggage(IEnumerable<KeyValuePair<string, IEnumerable<string>>> nonBaggage);

    /// <inheritdoc />
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

    /// <summary>
    /// Gets the current non-baggage items.
    /// </summary>
    /// <returns>The current non-baggage items.</returns>
    protected abstract IEnumerable<KeyValuePair<string, IEnumerable<string>>>? GetCurrentNonBaggage();
}

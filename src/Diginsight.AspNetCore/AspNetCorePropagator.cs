using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Diginsight.AspNetCore;

public sealed class AspNetCorePropagator : DiginsightPropagator
{
    private static readonly object NonBaggageItemsKey = new ();

    private readonly IHttpContextAccessor httpContextAccessor;

    private IDictionary<object, object?>? Items => httpContextAccessor.HttpContext?.Items;

    public AspNetCorePropagator(
        DistributedContextPropagator decoratee,
        IHttpContextAccessor httpContextAccessor,
        IOptions<DiginsightDistributedContextOptions> distributedContextOptions
    )
        : base(decoratee, distributedContextOptions.Value)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    protected override void SetCurrentNonBaggage(IEnumerable<KeyValuePair<string, IEnumerable<string>>> nonBaggage)
    {
        if (Items is not { } items)
            return;

        items[NonBaggageItemsKey] = nonBaggage;
    }

    protected override IEnumerable<KeyValuePair<string, IEnumerable<string>>>? GetCurrentNonBaggage()
    {
        return Items?.TryGetValue(NonBaggageItemsKey, out object? nonBaggage) == true
            ? nonBaggage as IEnumerable<KeyValuePair<string, IEnumerable<string>>>
            : null;
    }
}

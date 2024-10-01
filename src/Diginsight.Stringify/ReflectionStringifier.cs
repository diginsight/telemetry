using System.Diagnostics;

namespace Diginsight.Stringify;

internal abstract class ReflectionStringifier : IStringifier
{
    public IStringifiable? TryStringify(object obj)
    {
        Type type = obj.GetType();
        return IsHandled(type) switch
        {
            Handling.Pass => null,
            Handling.Handle => MakeStringifiable(obj),
            Handling.Forbid => new NonStringifiable(type),
            _ => throw new UnreachableException($"Unrecognized {nameof(Handling)}"),
        };
    }

    protected abstract Handling IsHandled(Type type);

    protected abstract ReflectionStringifiable MakeStringifiable(object obj);

    protected enum Handling
    {
        Pass,
        Handle,
        Forbid,
    }
}

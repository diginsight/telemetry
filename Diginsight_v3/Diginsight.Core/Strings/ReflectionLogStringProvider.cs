using System.Diagnostics;

namespace Diginsight.Strings;

internal abstract class ReflectionLogStringProvider : ILogStringProvider
{
    public ILogStringable? TryAsLogStringable(object obj)
    {
        Type type = obj.GetType();
        return IsHandled(type) switch
        {
            Handling.Pass => null,
            Handling.Handle => MakeLogStringable(obj),
            Handling.Forbid => new NonLogStringable(type),
            _ => throw new UnreachableException($"Unrecognized {nameof(Handling)}"),
        };
    }

    protected abstract Handling IsHandled(Type type);

    protected abstract ReflectionLogStringable MakeLogStringable(object obj);

    protected enum Handling : byte
    {
        Pass,
        Handle,
        Forbid,
    }
}

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Strings;

internal abstract class ReflectionLogStringProvider : ILogStringProvider
{
    public bool TryAsLogStringable(object obj, [NotNullWhen(true)] out ILogStringable? logStringable)
    {
        Type type = obj.GetType();

        logStringable = IsHandled(type) switch
        {
            Handling.Pass => null,
            Handling.Handle => MakeLogStringable(obj),
            Handling.Forbid => new NonLogStringable(type),
            _ => throw new UnreachableException($"Unrecognized {nameof(Handling)}"),
        };

        return logStringable is not null;
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

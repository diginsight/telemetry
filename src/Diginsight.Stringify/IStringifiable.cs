namespace Diginsight.Stringify;

public interface IStringifiable
{
    bool IsDeep
#if NET || NETSTANDARD2_1_OR_GREATER
        => true;
#else
    {
        get;
    }
#endif

    object? Subject { get; }

    void AppendTo(StringifyContext stringifyContext);
}

namespace Diginsight.Stringify;

public interface IStringifiable
{
#if NET || NETSTANDARD2_1_OR_GREATER
    bool IsDeep => true;
#else
    bool IsDeep { get; }
#endif

    object? Subject { get; }

    void AppendTo(StringifyContext stringifyContext);
}

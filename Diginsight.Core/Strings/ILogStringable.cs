namespace Diginsight.Strings;

public interface ILogStringable
{
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    bool IsDeep => true;
#else
    bool IsDeep { get; }
#endif

    object? Subject { get; }

    void AppendTo(AppendingContext appendingContext);
}

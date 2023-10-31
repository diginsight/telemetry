namespace Diginsight.Strings;

public interface ILogStringable
{
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    bool IsDeep => true;
    bool CanCycle => true;
#else
    bool IsDeep { get; }
    bool CanCycle { get; }
#endif

    void AppendTo(AppendingContext appendingContext);
}

namespace Diginsight.Diagnostics.TextWriting;

public interface IObservabilityTextWritingOptions
{
    bool UseUtcTimestamp { get; }

    LineDescriptor GetLineDescriptor(int? width, IEnumerable<ILineTokenParser>? customLineTokenParsers = null);
}

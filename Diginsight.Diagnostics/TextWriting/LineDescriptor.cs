namespace Diginsight.Diagnostics.TextWriting;

public readonly struct LineDescriptor
{
    private static readonly IEnumerable<ILineToken> DefaultLineTokensCore =
    [
        new TimestampToken(),
        new CategoryToken(),
        new LogLevelToken(),
        TraceIdToken.Instance,
        DeltaToken.Instance,
        DurationToken.Instance,
        DepthToken.Instance,
        new IndentationToken(),
        new MessageToken(),
    ];

    private static readonly IEnumerable<IPrefixTokenAppender> DefaultAppenders = Apply(DefaultLineTokensCore).Appenders;

    public static IEnumerable<ILineToken> DefaultLineTokens => DefaultLineTokensCore.Select(static x => x.Clone());

    private readonly IEnumerable<IPrefixTokenAppender>? appenders;

    public IEnumerable<IPrefixTokenAppender> Appenders => appenders ?? DefaultAppenders;

    public int MaxIndentedDepth { get; }

    public int MaxMessageLength { get; }

    public int MaxLineLength { get; }

    public LineDescriptor(IEnumerable<ILineToken> lineTokens)
        : this(lineTokens.ToArray(), true) { }

    internal LineDescriptor(IEnumerable<ILineToken> lineTokens, bool validate)
    {
        if (validate)
        {
            LineDescriptorParser.Validate(lineTokens);
        }

        MutableLineDescriptor descriptor = Apply(lineTokens);

        appenders = descriptor.Appenders;
        MaxIndentedDepth = descriptor.MaxIndentedDepth ?? 0;
        MaxMessageLength = descriptor.MaxMessageLength ?? 0;
        MaxLineLength = descriptor.MaxLineLength ?? 0;
    }

    private static MutableLineDescriptor Apply(IEnumerable<ILineToken> lineTokens)
    {
        MutableLineDescriptor descriptor = new ();
        foreach (ILineToken lineToken in lineTokens)
        {
            lineToken.Apply(ref descriptor);
        }
        return descriptor;
    }
}

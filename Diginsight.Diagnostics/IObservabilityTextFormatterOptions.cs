﻿namespace Diginsight.Diagnostics;

public interface IObservabilityTextFormatterOptions
{
    bool UseUtcTimestamp { get; }

    string? TimestampFormat { get; }

    string? TimestampCulture { get; }

    int CategoryLength { get; }

    int MaxMessageLength { get; }

    int MaxLineLength { get; }

    int MaxIndentedDepth { get; }
}
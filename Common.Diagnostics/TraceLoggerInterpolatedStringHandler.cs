
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Common
{
    public interface IProvideFormatStringParameters
    {
        StringBuilder FormatTemplate { get; }
        object[] FormatParameters { get; }
    }

#if NET6_0_OR_GREATER

    internal ref struct InterpolatedStringHandlerBase
    {
        public StringBuilder FormatTemplate { get; }
        public object[] FormatParameters { get; }

        public readonly bool IsEnabled;

        int i = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InterpolatedStringHandlerBase(LogLevel currentLevel, LogLevel logMinimumLevel, int literalLength, int formattedCount, out bool isEnabled)
        {
            this.IsEnabled = isEnabled = currentLevel >= logMinimumLevel;
            this.FormatTemplate = isEnabled ? new StringBuilder(literalLength + 3 * formattedCount) : default!;
            this.FormatParameters = isEnabled ? new object[formattedCount] : default!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void AppendLiteral(string s)=> this.FormatTemplate.Append(s.Replace("{", "{{").Replace("}", "}}"));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendFormatted<T>(T t)
        {
            this.FormatTemplate.Append($"{{{i}}}");
            this.FormatParameters[i] = t;
            i++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendFormatted<T>(T t, string format)
        {
            this.FormatTemplate.Append(t is IFormattable ? $"{{{i}:{format}}}" : $"{{{i}}}");
            this.FormatParameters[i] = t;
            i++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly string GetFormattedText() => string.Format(this.FormatTemplate.ToString(), this.FormatParameters);

    }


    [InterpolatedStringHandler]
    public ref struct TraceLoggerInterpolatedStringHandler
    {
        public readonly StringBuilder FormatTemplate => handler.FormatTemplate;
        public readonly object[] FormatParameters => handler.FormatParameters;

        private InterpolatedStringHandlerBase handler;
        public readonly LogLevel Level = LogLevel.Trace;

        public TraceLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    out bool isEnabled,
                                                    [CallerMemberName] string memberName = "",
                                                    [CallerFilePath] string sourceFilePath = "",
                                                    [CallerLineNumber] int sourceLineNumber = 0)
            : this(literalLength,
                   formattedCount,
                   CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection()
                                                          : new CodeSection(typeof(InternalClass),
                                                                            null,
                                                                            null,
                                                                            null,
                                                                            SourceLevels.Verbose,
                                                                            null,
                                                                            null,
                                                                            null,
                                                                            TraceManager.Stopwatch.ElapsedTicks,
                                                                            memberName,
                                                                            sourceFilePath,
                                                                            sourceLineNumber,
                                                                            true),
                   out isEnabled)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TraceLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    ICodeSection codeSection,
                                                    out bool isEnabled)
        {
            handler = new InterpolatedStringHandlerBase(Level,
                                                        codeSection.MinimumLogLevel,
                                                        literalLength,
                                                        formattedCount,
                                                        out isEnabled);
        }

        public TraceLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    ILogger logger,
                                                    out bool isEnabled,
                                                    [CallerMemberName] string memberName = "",
                                                    [CallerFilePath] string sourceFilePath = "",
                                                    [CallerLineNumber] int sourceLineNumber = 0)
            : this(literalLength,
                   formattedCount,
                   CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection()
                                                          : new CodeSectionScope(logger,
                                                                                 null,
                                                                                 typeof(InternalClass),
                                                                                 null,
                                                                                 null,
                                                                                 null,
                                                                                 SourceLevels.Verbose,
                                                                                 LogLevel.Trace,
                                                                                 null,
                                                                                 null,
                                                                                 null,
                                                                                 TraceManager.Stopwatch.ElapsedTicks,
                                                                                 memberName,
                                                                                 sourceFilePath,
                                                                                 sourceLineNumber,
                                                                                 true),
                   out isEnabled)
        {
        }

        public readonly void AppendLiteral(string s) => handler.AppendLiteral(s);

        public void AppendFormatted<T>(T t) => handler.AppendFormatted<T>(t);
        public void AppendFormatted<T>(T t, string format) => handler.AppendFormatted<T>(t, format);

        public readonly string GetFormattedText() => handler.GetFormattedText();// string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
        public override readonly string ToString() => handler.GetFormattedText();// string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
    }

    [InterpolatedStringHandler]
    public ref struct DebugLoggerInterpolatedStringHandler
    {
        public readonly StringBuilder FormatTemplate => handler.FormatTemplate;
        public readonly object[] FormatParameters => handler.FormatParameters;

        private InterpolatedStringHandlerBase handler;
        public readonly LogLevel Level = LogLevel.Debug;

        public DebugLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    out bool isEnabled,
                                                    [CallerMemberName] string memberName = "",
                                                    [CallerFilePath] string sourceFilePath = "",
                                                    [CallerLineNumber] int sourceLineNumber = 0)
            : this(literalLength,
                   formattedCount,
                   CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection()
                                                          : new CodeSection(typeof(InternalClass),
                                                                            null,
                                                                            null,
                                                                            null,
                                                                            SourceLevels.Verbose,
                                                                            null,
                                                                            null,
                                                                            null,
                                                                            TraceManager.Stopwatch.ElapsedTicks,
                                                                            memberName,
                                                                            sourceFilePath,
                                                                            sourceLineNumber,
                                                                            true),
                   out isEnabled)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DebugLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    ICodeSection codeSection,
                                                    out bool isEnabled)
        {
            handler = new InterpolatedStringHandlerBase(Level,
                                                        codeSection.MinimumLogLevel,
                                                        literalLength,
                                                        formattedCount,
                                                        out isEnabled);
        }

        public DebugLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    ILogger logger,
                                                    out bool isEnabled,
                                                    [CallerMemberName] string memberName = "",
                                                    [CallerFilePath] string sourceFilePath = "",
                                                    [CallerLineNumber] int sourceLineNumber = 0)
            : this(literalLength,
                   formattedCount,
                   CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection()
                                                          : new CodeSectionScope(logger,
                                                                                 null,
                                                                                 typeof(InternalClass),
                                                                                 null,
                                                                                 null,
                                                                                 null,
                                                                                 SourceLevels.Verbose,
                                                                                 LogLevel.Trace,
                                                                                 null,
                                                                                 null,
                                                                                 null,
                                                                                 TraceManager.Stopwatch.ElapsedTicks,
                                                                                 memberName,
                                                                                 sourceFilePath,
                                                                                 sourceLineNumber,
                                                                                 true),
                   out isEnabled)
        {
        }

        public readonly void AppendLiteral(string s) => handler.AppendLiteral(s);

        public void AppendFormatted<T>(T t) => handler.AppendFormatted<T>(t);
        public void AppendFormatted<T>(T t, string format) => handler.AppendFormatted<T>(t, format);

        public readonly string GetFormattedText() => handler.GetFormattedText();
        public override readonly string ToString() => handler.GetFormattedText();
    }

    [InterpolatedStringHandler]
    public ref struct InformationLoggerInterpolatedStringHandler 
    {
        public readonly StringBuilder FormatTemplate => handler.FormatTemplate;
        public readonly object[] FormatParameters => handler.FormatParameters;

        private InterpolatedStringHandlerBase handler;
        public readonly LogLevel Level = LogLevel.Information;

        public InformationLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    out bool isEnabled,
                                                    [CallerMemberName] string memberName = "",
                                                    [CallerFilePath] string sourceFilePath = "",
                                                    [CallerLineNumber] int sourceLineNumber = 0)
            : this(literalLength,
                   formattedCount,
                   CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection() 
                                                          : new CodeSection(typeof(InternalClass),
                                                                            null,
                                                                            null,
                                                                            null,
                                                                            SourceLevels.Verbose,
                                                                            null,
                                                                            null,
                                                                            null,
                                                                            TraceManager.Stopwatch.ElapsedTicks,
                                                                            memberName,
                                                                            sourceFilePath,
                                                                            sourceLineNumber,
                                                                            true),
                   out isEnabled)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InformationLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    ICodeSection codeSection,
                                                    out bool isEnabled)
        {
            handler = new InterpolatedStringHandlerBase(Level,
                                                        codeSection.MinimumLogLevel,
                                                        literalLength,
                                                        formattedCount,
                                                        out isEnabled);
        }

        public InformationLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    ILogger logger,
                                                    out bool isEnabled,
                                                    [CallerMemberName] string memberName = "",
                                                    [CallerFilePath] string sourceFilePath = "",
                                                    [CallerLineNumber] int sourceLineNumber = 0)
            : this(literalLength,
                   formattedCount,
                   CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection() 
                                                          : new CodeSectionScope(logger,
                                                                                 null,
                                                                                 typeof(InternalClass),
                                                                                 null,
                                                                                 null,
                                                                                 null,
                                                                                 SourceLevels.Verbose,
                                                                                 LogLevel.Trace,
                                                                                 null,
                                                                                 null,
                                                                                 null,
                                                                                 TraceManager.Stopwatch.ElapsedTicks,
                                                                                 memberName,
                                                                                 sourceFilePath,
                                                                                 sourceLineNumber,
                                                                                 true),
                   out isEnabled)
        {
        }

        public readonly void AppendLiteral(string s) => handler.AppendLiteral(s);

        public void AppendFormatted<T>(T t) => handler.AppendFormatted<T>(t);
        public void AppendFormatted<T>(T t, string format) => handler.AppendFormatted<T>(t, format);

        public readonly string GetFormattedText() => handler.GetFormattedText();
        public override readonly string ToString() => handler.GetFormattedText();
    }

    [InterpolatedStringHandler]
    public ref struct WarningLoggerInterpolatedStringHandler
    {
        public readonly StringBuilder FormatTemplate => handler.FormatTemplate;
        public readonly object[] FormatParameters => handler.FormatParameters;

        private InterpolatedStringHandlerBase handler;
        public readonly LogLevel Level = LogLevel.Warning;

        public WarningLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    out bool isEnabled,
                                                    [CallerMemberName] string memberName = "",
                                                    [CallerFilePath] string sourceFilePath = "",
                                                    [CallerLineNumber] int sourceLineNumber = 0)
            : this(literalLength,
                   formattedCount,
                   CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection()
                                                          : new CodeSection(typeof(InternalClass),
                                                                            null,
                                                                            null,
                                                                            null,
                                                                            SourceLevels.Verbose,
                                                                            null,
                                                                            null,
                                                                            null,
                                                                            TraceManager.Stopwatch.ElapsedTicks,
                                                                            memberName,
                                                                            sourceFilePath,
                                                                            sourceLineNumber,
                                                                            true),
                   out isEnabled)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WarningLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    ICodeSection codeSection,
                                                    out bool isEnabled)
        {
            handler = new InterpolatedStringHandlerBase(Level,
                                                        codeSection.MinimumLogLevel,
                                                        literalLength,
                                                        formattedCount,
                                                        out isEnabled);
        }

        public WarningLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    ILogger logger,
                                                    out bool isEnabled,
                                                    [CallerMemberName] string memberName = "",
                                                    [CallerFilePath] string sourceFilePath = "",
                                                    [CallerLineNumber] int sourceLineNumber = 0)
            : this(literalLength,
                   formattedCount,
                   CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection()
                                                          : new CodeSectionScope(logger,
                                                                                 null,
                                                                                 typeof(InternalClass),
                                                                                 null,
                                                                                 null,
                                                                                 null,
                                                                                 SourceLevels.Verbose,
                                                                                 LogLevel.Trace,
                                                                                 null,
                                                                                 null,
                                                                                 null,
                                                                                 TraceManager.Stopwatch.ElapsedTicks,
                                                                                 memberName,
                                                                                 sourceFilePath,
                                                                                 sourceLineNumber,
                                                                                 true),
                   out isEnabled)
        {
        }

        public readonly void AppendLiteral(string s) => handler.AppendLiteral(s);

        public void AppendFormatted<T>(T t) => handler.AppendFormatted<T>(t);
        public void AppendFormatted<T>(T t, string format) => handler.AppendFormatted<T>(t, format);

        public readonly string GetFormattedText() => handler.GetFormattedText();
        public override readonly string ToString() => handler.GetFormattedText();
    }

    [InterpolatedStringHandler]
    public ref struct ErrorLoggerInterpolatedStringHandler
    {
        public readonly StringBuilder FormatTemplate => handler.FormatTemplate;
        public readonly object[] FormatParameters => handler.FormatParameters;

        private InterpolatedStringHandlerBase handler;
        public readonly LogLevel Level = LogLevel.Error;

        public ErrorLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    out bool isEnabled,
                                                    [CallerMemberName] string memberName = "",
                                                    [CallerFilePath] string sourceFilePath = "",
                                                    [CallerLineNumber] int sourceLineNumber = 0)
            : this(literalLength,
                   formattedCount,
                   CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection()
                                                          : new CodeSection(typeof(InternalClass),
                                                                            null,
                                                                            null,
                                                                            null,
                                                                            SourceLevels.Verbose,
                                                                            null,
                                                                            null,
                                                                            null,
                                                                            TraceManager.Stopwatch.ElapsedTicks,
                                                                            memberName,
                                                                            sourceFilePath,
                                                                            sourceLineNumber,
                                                                            true),
                   out isEnabled)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ErrorLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    ICodeSection codeSection,
                                                    out bool isEnabled)
        {
            handler = new InterpolatedStringHandlerBase(Level,
                                                        codeSection.MinimumLogLevel,
                                                        literalLength,
                                                        formattedCount,
                                                        out isEnabled);
        }

        public ErrorLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    ILogger logger,
                                                    out bool isEnabled,
                                                    [CallerMemberName] string memberName = "",
                                                    [CallerFilePath] string sourceFilePath = "",
                                                    [CallerLineNumber] int sourceLineNumber = 0)
            : this(literalLength,
                   formattedCount,
                   CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection()
                                                          : new CodeSectionScope(logger,
                                                                                 null,
                                                                                 typeof(InternalClass),
                                                                                 null,
                                                                                 null,
                                                                                 null,
                                                                                 SourceLevels.Verbose,
                                                                                 LogLevel.Trace,
                                                                                 null,
                                                                                 null,
                                                                                 null,
                                                                                 TraceManager.Stopwatch.ElapsedTicks,
                                                                                 memberName,
                                                                                 sourceFilePath,
                                                                                 sourceLineNumber,
                                                                                 true),
                   out isEnabled)
        {
        }

        public readonly void AppendLiteral(string s) => handler.AppendLiteral(s);

        public void AppendFormatted<T>(T t) => handler.AppendFormatted<T>(t);
        public void AppendFormatted<T>(T t, string format) => handler.AppendFormatted<T>(t, format);

        public readonly string GetFormattedText() => handler.GetFormattedText();
        public override readonly string ToString() => handler.GetFormattedText();
    }

    [InterpolatedStringHandler]
    public ref struct CriticalLoggerInterpolatedStringHandler
    {
        public readonly StringBuilder FormatTemplate => handler.FormatTemplate;
        public readonly object[] FormatParameters => handler.FormatParameters;

        private InterpolatedStringHandlerBase handler;
        public readonly LogLevel Level = LogLevel.Critical;

        public CriticalLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    out bool isEnabled,
                                                    [CallerMemberName] string memberName = "",
                                                    [CallerFilePath] string sourceFilePath = "",
                                                    [CallerLineNumber] int sourceLineNumber = 0)
            : this(literalLength,
                   formattedCount,
                   CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection()
                                                          : new CodeSection(typeof(InternalClass),
                                                                            null,
                                                                            null,
                                                                            null,
                                                                            SourceLevels.Verbose,
                                                                            null,
                                                                            null,
                                                                            null,
                                                                            TraceManager.Stopwatch.ElapsedTicks,
                                                                            memberName,
                                                                            sourceFilePath,
                                                                            sourceLineNumber,
                                                                            true),
                   out isEnabled)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CriticalLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    ICodeSection codeSection,
                                                    out bool isEnabled)
        {
            handler = new InterpolatedStringHandlerBase(Level,
                                                        codeSection.MinimumLogLevel,
                                                        literalLength,
                                                        formattedCount,
                                                        out isEnabled);
        }

        public CriticalLoggerInterpolatedStringHandler(int literalLength,
                                                    int formattedCount,
                                                    ILogger logger,
                                                    out bool isEnabled,
                                                    [CallerMemberName] string memberName = "",
                                                    [CallerFilePath] string sourceFilePath = "",
                                                    [CallerLineNumber] int sourceLineNumber = 0)
            : this(literalLength,
                   formattedCount,
                   CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection()
                                                          : new CodeSectionScope(logger,
                                                                                 null,
                                                                                 typeof(InternalClass),
                                                                                 null,
                                                                                 null,
                                                                                 null,
                                                                                 SourceLevels.Verbose,
                                                                                 LogLevel.Trace,
                                                                                 null,
                                                                                 null,
                                                                                 null,
                                                                                 TraceManager.Stopwatch.ElapsedTicks,
                                                                                 memberName,
                                                                                 sourceFilePath,
                                                                                 sourceLineNumber,
                                                                                 true),
                   out isEnabled)
        {
        }

        public readonly void AppendLiteral(string s) => handler.AppendLiteral(s);

        public void AppendFormatted<T>(T t) => handler.AppendFormatted<T>(t);
        public void AppendFormatted<T>(T t, string format) => handler.AppendFormatted<T>(t, format);

        public readonly string GetFormattedText() => handler.GetFormattedText();// string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
        public override readonly string ToString() => handler.GetFormattedText();// string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
    }



    //[InterpolatedStringHandler]
    //public ref struct DebugLoggerInterpolatedStringHandler
    //{
    //    public StringBuilder FormatTemplate { get; }
    //    public object[] FormatParameters { get; }

    //    public readonly LogLevel Level = LogLevel.Debug;

    //    int i = 0;
    //    public DebugLoggerInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    //        : this(literalLength, formattedCount, CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection() : new CodeSection(typeof(InternalClass), null, null, null, SourceLevels.Verbose, null, null, null, TraceManager.Stopwatch.ElapsedTicks, memberName, sourceFilePath, sourceLineNumber, true), out isEnabled)
    //    {
    //    }

    //    public DebugLoggerInterpolatedStringHandler(int literalLength, int formattedCount, LogLevel minimumLevel, out bool isEnabled) // isEnalbed
    //    {
    //        isEnabled = Level >= minimumLevel;
    //        this.FormatTemplate = isEnabled ? new StringBuilder(literalLength + 3 * formattedCount) : default!;
    //        this.FormatParameters = isEnabled ? new object[formattedCount] : default!;
    //    }

    //    public DebugLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ICodeSection codeSection, out bool isEnabled) // isEnalbed
    //        : this(literalLength, formattedCount, codeSection.MinimumLogLevel, out isEnabled)
    //    {
    //    }

    //    public DebugLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) // isEnalbed
    //        : this(literalLength, formattedCount, CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection() : new CodeSectionScope(logger, null, typeof(InternalClass), null, null, null, SourceLevels.Verbose, LogLevel.Trace, null, null, null, TraceManager.Stopwatch.ElapsedTicks, memberName, sourceFilePath, sourceLineNumber, true), out isEnabled)
    //    {
    //    }

    //    public void AppendLiteral(string s)
    //    {
    //        this.FormatTemplate.Append(s.Replace("{", "{{").Replace("}", "}}"));
    //    }

    //    public void AppendFormatted<T>(T t)
    //    {
    //        this.FormatTemplate.Append($"{{{i}}}");
    //        this.FormatParameters[i] = t;
    //        i++;
    //    }
    //    public void AppendFormatted<T>(T t, string format)
    //    {
    //        this.FormatTemplate.Append(t is IFormattable ? $"{{{i}:{format}}}" : $"{{{i}}}");
    //        this.FormatParameters[i] = t;
    //        i++;
    //    }

    //    public string GetFormattedText() => string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
    //    override public string ToString() => string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
    //}

    //[InterpolatedStringHandler]
    //public ref struct InformationLoggerInterpolatedStringHandler
    //{
    //    public StringBuilder FormatTemplate { get; }
    //    public object[] FormatParameters { get; }

    //    public readonly LogLevel Level = LogLevel.Information;

    //    int i = 0;
    //    public InformationLoggerInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    //        : this(literalLength, formattedCount, CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection() : new CodeSection(typeof(InternalClass), null, null, null, SourceLevels.Verbose, null, null, null, TraceManager.Stopwatch.ElapsedTicks, memberName, sourceFilePath, sourceLineNumber, true), out isEnabled)
    //    {
    //    }

    //    public InformationLoggerInterpolatedStringHandler(int literalLength, int formattedCount, LogLevel minimumLevel, out bool isEnabled) // isEnalbed
    //    {
    //        isEnabled = Level >= minimumLevel;
    //        this.FormatTemplate = isEnabled ? new StringBuilder(literalLength + 3 * formattedCount) : default!;
    //        this.FormatParameters = isEnabled ? new object[formattedCount] : default!;
    //    }

    //    public InformationLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ICodeSection codeSection, out bool isEnabled) // isEnalbed
    //        : this(literalLength, formattedCount, codeSection.MinimumLogLevel, out isEnabled)
    //    {
    //    }

    //    public InformationLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) // isEnalbed
    //        : this(literalLength, formattedCount, CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection() : new CodeSectionScope(logger, null, typeof(InternalClass), null, null, null, SourceLevels.Verbose, LogLevel.Trace, null, null, null, TraceManager.Stopwatch.ElapsedTicks, memberName, sourceFilePath, sourceLineNumber, true), out isEnabled)
    //    {
    //    }


    //    public void AppendLiteral(string s)
    //    {
    //        this.FormatTemplate.Append(s.Replace("{", "{{").Replace("}", "}}"));
    //    }

    //    public void AppendFormatted<T>(T t)
    //    {
    //        this.FormatTemplate.Append($"{{{i}}}");
    //        this.FormatParameters[i] = t;
    //        i++;
    //    }
    //    public void AppendFormatted<T>(T t, string format)
    //    {
    //        this.FormatTemplate.Append(t is IFormattable ? $"{{{i}:{format}}}" : $"{{{i}}}");
    //        this.FormatParameters[i] = t;
    //        i++;
    //    }

    //    public string GetFormattedText() => string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
    //    override public string ToString() => string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
    //}

    //[InterpolatedStringHandler]
    //public ref struct WarningLoggerInterpolatedStringHandler
    //{
    //    public StringBuilder FormatTemplate { get; }
    //    public object[] FormatParameters { get; }

    //    public readonly LogLevel Level = LogLevel.Warning;

    //    int i = 0;
    //    public WarningLoggerInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    //        : this(literalLength, formattedCount, CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection() : new CodeSection(typeof(InternalClass), null, null, null, SourceLevels.Verbose, null, null, null, TraceManager.Stopwatch.ElapsedTicks, memberName, sourceFilePath, sourceLineNumber, true), out isEnabled)
    //    {
    //    }

    //    public WarningLoggerInterpolatedStringHandler(int literalLength, int formattedCount, LogLevel minimumLevel, out bool isEnabled) // isEnalbed
    //    {
    //        isEnabled = Level >= minimumLevel;
    //        this.FormatTemplate = isEnabled ? new StringBuilder(literalLength + 3 * formattedCount) : default!;
    //        this.FormatParameters = isEnabled ? new object[formattedCount] : default!;
    //    }

    //    public WarningLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ICodeSection codeSection, out bool isEnabled) // isEnalbed
    //        : this(literalLength, formattedCount, codeSection.MinimumLogLevel, out isEnabled)
    //    {
    //    }

    //    public WarningLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) // isEnalbed
    //        : this(literalLength, formattedCount, CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection() : new CodeSectionScope(logger, null, typeof(InternalClass), null, null, null, SourceLevels.Verbose, LogLevel.Trace, null, null, null, TraceManager.Stopwatch.ElapsedTicks, memberName, sourceFilePath, sourceLineNumber, true), out isEnabled)
    //    {
    //    }


    //    public void AppendLiteral(string s)
    //    {
    //        this.FormatTemplate.Append(s.Replace("{", "{{").Replace("}", "}}"));
    //    }

    //    public void AppendFormatted<T>(T t)
    //    {
    //        this.FormatTemplate.Append($"{{{i}}}");
    //        this.FormatParameters[i] = t;
    //        i++;
    //    }
    //    public void AppendFormatted<T>(T t, string format)
    //    {
    //        this.FormatTemplate.Append(t is IFormattable ? $"{{{i}:{format}}}" : $"{{{i}}}");
    //        this.FormatParameters[i] = t;
    //        i++;
    //    }

    //    public string GetFormattedText() => string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
    //    override public string ToString() => string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
    //}

    //[InterpolatedStringHandler]
    //public ref struct ErrorLoggerInterpolatedStringHandler
    //{
    //    public StringBuilder FormatTemplate { get; }
    //    public object[] FormatParameters { get; }

    //    public readonly LogLevel Level = LogLevel.Error;

    //    int i = 0;
    //    public ErrorLoggerInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    //        : this(literalLength, formattedCount, CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection() : new CodeSection(typeof(InternalClass), null, null, null, SourceLevels.Verbose, null, null, null, TraceManager.Stopwatch.ElapsedTicks, memberName, sourceFilePath, sourceLineNumber, true), out isEnabled)
    //    {
    //    }

    //    public ErrorLoggerInterpolatedStringHandler(int literalLength, int formattedCount, LogLevel minimumLevel, out bool isEnabled) // isEnalbed
    //    {
    //        isEnabled = Level >= minimumLevel;
    //        this.FormatTemplate = isEnabled ? new StringBuilder(literalLength + 3 * formattedCount) : default!;
    //        this.FormatParameters = isEnabled ? new object[formattedCount] : default!;
    //    }

    //    public ErrorLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ICodeSection codeSection, out bool isEnabled) // isEnalbed
    //        : this(literalLength, formattedCount, codeSection.MinimumLogLevel, out isEnabled)
    //    {
    //    }

    //    public ErrorLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) // isEnalbed
    //        : this(literalLength, formattedCount, CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection() : new CodeSectionScope(logger, null, typeof(InternalClass), null, null, null, SourceLevels.Verbose, LogLevel.Trace, null, null, null, TraceManager.Stopwatch.ElapsedTicks, memberName, sourceFilePath, sourceLineNumber, true), out isEnabled)
    //    {
    //    }


    //    public void AppendLiteral(string s)
    //    {
    //        this.FormatTemplate.Append(s.Replace("{", "{{").Replace("}", "}}"));
    //    }

    //    public void AppendFormatted<T>(T t)
    //    {
    //        this.FormatTemplate.Append($"{{{i}}}");
    //        this.FormatParameters[i] = t;
    //        i++;
    //    }
    //    public void AppendFormatted<T>(T t, string format)
    //    {
    //        this.FormatTemplate.Append(t is IFormattable ? $"{{{i}:{format}}}" : $"{{{i}}}");
    //        this.FormatParameters[i] = t;
    //        i++;
    //    }

    //    public string GetFormattedText() => string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
    //    override public string ToString() => string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
    //}

    //[InterpolatedStringHandler]
    //public ref struct CriticalLoggerInterpolatedStringHandler
    //{
    //    public StringBuilder FormatTemplate { get; }
    //    public object[] FormatParameters { get; }

    //    public readonly LogLevel Level = LogLevel.Critical;

    //    int i = 0;
    //    public CriticalLoggerInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
    //        : this(literalLength, formattedCount, CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection() : new CodeSection(typeof(InternalClass), null, null, null, SourceLevels.Verbose, null, null, null, TraceManager.Stopwatch.ElapsedTicks, memberName, sourceFilePath, sourceLineNumber, true), out isEnabled)
    //    {
    //    }

    //    public CriticalLoggerInterpolatedStringHandler(int literalLength, int formattedCount, LogLevel minimumLevel, out bool isEnabled) // isEnalbed
    //    {
    //        isEnabled = Level >= minimumLevel;
    //        this.FormatTemplate = isEnabled ? new StringBuilder(literalLength + 3 * formattedCount) : default!;
    //        this.FormatParameters = isEnabled ? new object[formattedCount] : default!;
    //    }

    //    public CriticalLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ICodeSection codeSection, out bool isEnabled) // isEnalbed
    //        : this(literalLength, formattedCount, codeSection.MinimumLogLevel, out isEnabled)
    //    {
    //    }

    //    public CriticalLoggerInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) // isEnalbed
    //        : this(literalLength, formattedCount, CodeSectionScope.Current.Value != null ? CodeSectionScope.Current.Value.GetInnerSection() : new CodeSectionScope(logger, null, typeof(InternalClass), null, null, null, SourceLevels.Verbose, LogLevel.Trace, null, null, null, TraceManager.Stopwatch.ElapsedTicks, memberName, sourceFilePath, sourceLineNumber, true), out isEnabled)
    //    {
    //    }


    //    public void AppendLiteral(string s)
    //    {
    //        this.FormatTemplate.Append(s.Replace("{", "{{").Replace("}", "}}"));
    //    }

    //    public void AppendFormatted<T>(T t)
    //    {
    //        this.FormatTemplate.Append($"{{{i}}}");
    //        this.FormatParameters[i] = t;
    //        i++;
    //    }
    //    public void AppendFormatted<T>(T t, string format)
    //    {
    //        this.FormatTemplate.Append(t is IFormattable ? $"{{{i}:{format}}}" : $"{{{i}}}");
    //        this.FormatParameters[i] = t;
    //        i++;
    //    }

    //    public string GetFormattedText() => string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
    //    override public string ToString() => string.Format(this.FormatTemplate.ToString(), this.FormatParameters);
    //}

#endif

}

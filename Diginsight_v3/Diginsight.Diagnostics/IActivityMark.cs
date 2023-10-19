namespace Diginsight.Diagnostics;

internal interface IActivityMark
{
    TimeSpan? Duration { get; }
}

namespace Diginsight.Diagnostics.Log4Net;

internal sealed record Log4NetMessage(string Message, bool IsActivity, TimeSpan? Duration);

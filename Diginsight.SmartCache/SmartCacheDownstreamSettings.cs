namespace Diginsight.SmartCache;

internal sealed class SmartCacheDownstreamSettings
{
    private readonly AsyncLocal<KeyValuePair<string, string>?> headerAsyncLocal = new ();

    public KeyValuePair<string, string>? Header => headerAsyncLocal.Value;

    public IDisposable WithZeroMaxAge()
    {
        return With(new KeyValuePair<string, string>(nameof(SmartCacheCoreOptions.MaxAge), "0"));
    }

    public IDisposable WithMinimumCreationDate(DateTimeOffset minimumCreationDate)
    {
        return With(new KeyValuePair<string, string>(nameof(SmartCacheCoreOptions.MinimumCreationDate), minimumCreationDate.ToString("O")));
    }

    private IDisposable With(KeyValuePair<string, string> header)
    {
        KeyValuePair<string, string>? previousHeader = headerAsyncLocal.Value;
        headerAsyncLocal.Value = header;
        return new CallbackDisposable(() => headerAsyncLocal.Value = previousHeader);
    }
}

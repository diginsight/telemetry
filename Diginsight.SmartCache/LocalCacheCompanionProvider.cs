namespace Diginsight.SmartCache;

internal class LocalCacheCompanionProvider : ICacheCompanionProvider
{
    public string SelfIp => "127.0.0.1";

    public Task<IEnumerable<string>> GetCompanionIpsAsync()
    {
        return Task.FromResult(Enumerable.Empty<string>());
    }
}

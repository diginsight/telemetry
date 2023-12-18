namespace Diginsight.SmartCache;

public interface ICacheCompanionProvider
{
    string SelfIp { get; }

    Task<IEnumerable<string>> GetCompanionIpsAsync();
}

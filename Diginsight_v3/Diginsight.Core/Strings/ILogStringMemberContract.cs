namespace Diginsight.Strings;

public interface ILogStringMemberContract
{
    bool? Included { get; }
    string? Name { get; }
    Type? ProviderType { get; }
}

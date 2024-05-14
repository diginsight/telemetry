namespace Diginsight.Strings;

public interface ILogStringableMemberDescriptor
{
    string? Name { get; }

    Type? ProviderType { get; }

    object[] ProviderArgs { get; }

    int? Order { get; }
}

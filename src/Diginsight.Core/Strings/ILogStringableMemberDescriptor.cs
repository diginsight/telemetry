namespace Diginsight.Strings;

public interface ILogStringableMemberDescriptor : ILogStringableDescriptor
{
    string? Name { get; }

    Type? ProviderType { get; }

    object[] ProviderArgs { get; }

    int? Order { get; }
}

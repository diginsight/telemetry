using System.Reflection;

namespace Diginsight;

internal sealed class MetadataMemberInfoEqualityComparer : IEqualityComparer<MemberInfo>
{
    public static readonly IEqualityComparer<MemberInfo> Instance = new MetadataMemberInfoEqualityComparer();

    private MetadataMemberInfoEqualityComparer() { }

    public bool Equals(MemberInfo? o1, MemberInfo? o2)
    {
        if (o1 == o2)
            return true;
        if (o1 is null || o2 is null)
            return false;
        return o1.HasSameMetadataDefinitionAs(o2);
    }

    public int GetHashCode(MemberInfo obj) => obj.GetHashCode();
}

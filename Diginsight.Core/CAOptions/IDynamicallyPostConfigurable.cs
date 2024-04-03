namespace Diginsight.CAOptions;

public interface IDynamicallyPostConfigurable
{
    object MakeFiller()
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        => this
#endif
        ;
}

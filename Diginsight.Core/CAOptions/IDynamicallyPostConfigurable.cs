namespace Diginsight.CAOptions;

public interface IDynamicallyPostConfigurable
{
    object MakeFiller()
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        => this
#endif
        ;
}

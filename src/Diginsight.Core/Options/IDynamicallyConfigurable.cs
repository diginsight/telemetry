namespace Diginsight.Options;

public interface IDynamicallyConfigurable
{
    object MakeFiller()
#if NET || NETSTANDARD2_1_OR_GREATER
        => this;
#else
        ;
#endif
}

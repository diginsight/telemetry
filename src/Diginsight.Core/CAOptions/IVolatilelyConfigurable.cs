﻿namespace Diginsight.CAOptions;

public interface IVolatilelyConfigurable
{
    object MakeFiller()
#if NET || NETSTANDARD2_1_OR_GREATER
        => this;
#else
        ;
#endif
}

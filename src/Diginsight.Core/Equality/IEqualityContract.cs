﻿namespace Diginsight.Equality;

public interface IEqualityContract
{
    EqualityBehavior? Behavior { get; }

    IComparerEquatableDescriptor? ComparerDescriptor { get; }

    IProxyEquatableDescriptor? ProxyDescriptor { get; }
}
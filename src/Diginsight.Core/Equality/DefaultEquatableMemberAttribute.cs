﻿namespace Diginsight.Equality;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class DefaultEquatableMemberAttribute : EquatableMemberAttribute
{
    public override EqualityBehavior Behavior => EqualityBehavior.Default;
}
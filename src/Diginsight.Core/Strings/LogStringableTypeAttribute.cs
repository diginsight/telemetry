﻿namespace Diginsight.Strings;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
public sealed class LogStringableTypeAttribute : Attribute, ILogStringableTypeDescriptor;

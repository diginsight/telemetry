﻿namespace Diginsight.Diagnostics.TextWriting;

public interface ILineToken
{
    void Apply(ref LineDescriptor lineDescriptor);
}
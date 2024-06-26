﻿using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight.CAOptions;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConfigurationExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IConfiguration FilterBy(this IConfiguration configuration, Type? @class) =>
        FilteredConfiguration.For(configuration, @class);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IConfiguration FilterByNoClass(this IConfiguration configuration) =>
        FilteredConfiguration.ForNoClass(configuration);
}

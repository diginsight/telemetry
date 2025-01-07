using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Diginsight.Diagnostics.AspNetCore;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    [Obsolete("Moved to `Diginsight.AspNetCore` namespace")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IWebHostBuilder UseDiginsightServiceProvider(
        this IWebHostBuilder hostBuilder,
        Action<WebHostBuilderContext, ServiceProviderOptions>? configureOptions = null
    )
    {
        return Diginsight.AspNetCore.DependencyInjectionExtensions.UseDiginsightServiceProvider(hostBuilder, false, configureOptions);
    }
}

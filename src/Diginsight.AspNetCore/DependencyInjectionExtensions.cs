using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.ComponentModel;
using System.Runtime.CompilerServices;
#if NET
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
#endif

namespace Diginsight.AspNetCore;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Configures the <see cref="IWebHostBuilder" /> to use the Diginsight service provider.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="validateInDevelopment">Indicates whether to validate service scopes in development environment.</param>
    /// <param name="configureOptions">The action to configure <see cref="ServiceProviderOptions" />.</param>
    /// <returns>The host builder, for chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IWebHostBuilder UseDiginsightServiceProvider(
        this IWebHostBuilder hostBuilder,
        bool validateInDevelopment = true,
        Action<WebHostBuilderContext, ServiceProviderOptions>? configureOptions = null
    )
    {
        return hostBuilder.ConfigureServices(
            (context, services) =>
            {
                bool validate = validateInDevelopment && context.HostingEnvironment.IsDevelopment();
                ServiceProviderOptions options = new () { ValidateScopes = validate, ValidateOnBuild = validate };
                configureOptions?.Invoke(context, options);
                services.Replace(ServiceDescriptor.Singleton<IServiceProviderFactory<IServiceCollection>>(new DiginsightServiceProviderFactory(options)));
            }
        );
    }

#if NET
    /// <summary>
    /// Configures the <see cref="WebApplicationBuilder" /> to use the Diginsight service provider.
    /// </summary>
    /// <param name="webAppBuilder">The web application builder.</param>
    /// <param name="validateInDevelopment">Indicates whether to validate service scopes in development environment.</param>
    /// <param name="configureOptions">The action to configure <see cref="ServiceProviderOptions" />.</param>
    /// <returns>The web application builder, for chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WebApplicationBuilder UseDiginsightServiceProvider(
        this WebApplicationBuilder webAppBuilder,
        bool validateInDevelopment = true,
        Action<HostBuilderContext, ServiceProviderOptions>? configureOptions = null
    )
    {
        webAppBuilder.Host.UseDiginsightServiceProvider(validateInDevelopment, configureOptions);
        return webAppBuilder;
    }
#endif
}

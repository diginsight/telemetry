using Microsoft.Extensions.Hosting;

namespace Diginsight;

/// <summary>
/// Defines an injectable action to be run upon service provider creation.
/// </summary>
/// <remarks>
/// In order for the action to be run, the <see cref="DependencyInjectionExtensions.UseDiginsightServiceProvider(Microsoft.Extensions.Hosting.IHostBuilder,bool,System.Action{Microsoft.Extensions.Hosting.HostBuilderContext,Microsoft.Extensions.DependencyInjection.ServiceProviderOptions}?)" />
/// must be called in the configuration phase of the <see cref="IHostBuilder" />.
/// </remarks>
public interface IOnCreateServiceProvider
{
    /// <summary>
    /// Executes the action.
    /// </summary>
    void Run();
}

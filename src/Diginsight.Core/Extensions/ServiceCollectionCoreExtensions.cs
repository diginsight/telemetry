using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;

namespace Diginsight;

// Extension methods for multi-targeting
public static class ServiceCollectionCoreExtensions
{
#if NET8_0_OR_GREATER
    public static IServiceCollection AddNamedSingleton<TInterface, TImplementation>(
        this IServiceCollection services, 
        string name)
        where TImplementation : class, TInterface
        where TInterface : class
    {
        return services.AddKeyedSingleton<TInterface, TImplementation>(name);
    }

    public static IServiceCollection AddNamedSingleton<TInterface, TImplementation>(
        this IServiceCollection services,
        string name,
        Func<IServiceProvider, string, TInterface> factory)
        where TImplementation : class, TInterface
        where TInterface : class
    {
        return services.AddKeyedSingleton<TInterface>(name, (sp, key) => factory(sp, key?.ToString() ?? string.Empty));
    }

    public static IServiceCollection DecorateNamed<TInterface, TDecorator>(
        this IServiceCollection services,
        string name)
        where TDecorator : class, TInterface
        where TInterface : class
    {
        // Store the original registration temporarily
        var tempKey = $"_temp_{name}_{Guid.NewGuid():N}";
        
        // Find the existing keyed service
        var existingDescriptor = services.FirstOrDefault(d => 
            d.ServiceType == typeof(TInterface) && 
            d.ServiceKey?.ToString() == name);
        
        if (existingDescriptor == null)
            throw new InvalidOperationException($"No keyed service found for {typeof(TInterface).Name} with key '{name}'");

        // Remove the original registration
        services.Remove(existingDescriptor);
        
        // Re-register the original with a temporary key
        services.AddKeyedSingleton<TInterface>(tempKey, (sp, _) => (TInterface) existingDescriptor.ImplementationFactory!(sp));

        
        // Register the decorator with the original key
        services.AddKeyedSingleton<TInterface>(name, (sp, key) =>
        {
            var originalService = sp.GetRequiredKeyedService<TInterface>(tempKey);
            return ActivatorUtilities.CreateInstance<TDecorator>(sp, originalService);
        });

        return services;
    }

    public static T GetNamedService<T>(this IServiceProvider serviceProvider, string name)
        where T : class
    {
        return serviceProvider.GetRequiredKeyedService<T>(name);
    }
#else
    public static IServiceCollection AddNamedSingleton<TInterface, TImplementation>(
        this IServiceCollection services,
        string name)
        where TImplementation : class, TInterface
        where TInterface : class
    {
        services.TryAddSingleton<INamedServiceRegistry<TInterface>, NamedServiceRegistry<TInterface>>();
        services.Configure<NamedServiceRegistryOptions<TInterface>>(options =>
        {
            options.RegisterFactory(name, sp => ActivatorUtilities.CreateInstance<TImplementation>(sp));
        });
        return services;
    }

    public static IServiceCollection AddNamedSingleton<TInterface, TImplementation>(
        this IServiceCollection services,
        string name,
        Func<IServiceProvider, string, TInterface> factory)
        where TImplementation : class, TInterface
        where TInterface : class
    {
        services.TryAddSingleton<INamedServiceRegistry<TInterface>, NamedServiceRegistry<TInterface>>();
        services.Configure<NamedServiceRegistryOptions<TInterface>>(options =>
        {
            options.RegisterFactory(name, sp => factory(sp, name));
        });
        return services;
    }

    public static IServiceCollection DecorateNamed<TInterface, TDecorator>(
        this IServiceCollection services,
        string name)
        where TDecorator : class, TInterface
        where TInterface : class
    {
        services.TryAddSingleton<INamedServiceRegistry<TInterface>, NamedServiceRegistry<TInterface>>();
        services.Configure<NamedServiceRegistryOptions<TInterface>>(options =>
        {
            var originalFactory = options.GetFactory(name);
            if (originalFactory == null)
                throw new InvalidOperationException($"No named service found for {typeof(TInterface).Name} with name '{name}'");

            options.ReplaceFactory(name, sp =>
            {
                var originalService = originalFactory(sp);
                return ActivatorUtilities.CreateInstance<TDecorator>(sp, originalService);
            });
        });
        return services;
    }

    public static T GetNamedService<T>(this IServiceProvider serviceProvider, string name)
        where T : class
    {
        var registry = serviceProvider.GetRequiredService<INamedServiceRegistry<T>>();
        return registry.GetService(name);
    }
#endif
}

// Supporting types for older versions
#if !NET8_0_OR_GREATER
public interface INamedServiceRegistry<T>
{
    T GetService(string name);
}

public class NamedServiceRegistryOptions<T>
{
    private readonly Dictionary<string, Func<IServiceProvider, T>> _factories = new();

    public void RegisterFactory(string name, Func<IServiceProvider, T> factory)
    {
        _factories[name] = factory;
    }

    public void ReplaceFactory(string name, Func<IServiceProvider, T> factory)
    {
        _factories[name] = factory;
    }

    public Func<IServiceProvider, T>? GetFactory(string name)
    {
        return _factories.TryGetValue(name, out var factory) ? factory : null;
    }

    internal IReadOnlyDictionary<string, Func<IServiceProvider, T>> Factories => _factories;
}

public class NamedServiceRegistry<T> : INamedServiceRegistry<T>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly NamedServiceRegistryOptions<T> _options;

    public NamedServiceRegistry(IServiceProvider serviceProvider, IOptions<NamedServiceRegistryOptions<T>> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public T GetService(string name)
    {
        if (_options.Factories.TryGetValue(name, out var factory))
            return factory(_serviceProvider);
        throw new InvalidOperationException($"No service registered for name: {name}");
    }
}
#endif


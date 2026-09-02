using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Diagnostics;

/// <summary>
/// Represents a manager for deferred early logging before the service provider is available.
/// </summary>
public class EarlyLoggingManager : IDisposable
{
    private readonly DeferredOperationRegistry operationRegistry;
    private readonly DeferredLoggerFactory loggerFactory;
    private readonly DeferredActivityLifecycleLogEmitter logEmitter;

    /// <summary>
    /// The service collection attached to this manager.
    /// </summary>
    protected IServiceCollection? services;

    private ILoggerFactory? emergencyLoggerFactory;
    private ActivityLifecycleLogEmitter? emergencyLogEmitter;

    /// <summary>
    /// Gets the deferred logger factory used for early logging.
    /// </summary>
    public ILoggerFactory LoggerFactory => loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EarlyLoggingManager" /> class.
    /// </summary>
    /// <param name="shouldListenTo">The predicate used to determine whether an activity source should be listened to.</param>
    /// <param name="timeProvider">The time provider used to timestamp deferred operations.</param>
    public EarlyLoggingManager(Func<ActivitySource, bool> shouldListenTo, TimeProvider? timeProvider = null)
    {
        operationRegistry = new DeferredOperationRegistry();

        loggerFactory = new DeferredLoggerFactory(operationRegistry, timeProvider, GetEmergencyLoggerFactory);
        logEmitter = new DeferredActivityLifecycleLogEmitter(operationRegistry, shouldListenTo, timeProvider, GetEmergencyLogEmitter);
    }

    /// <summary>
    /// Gets the emergency logger factory used when deferred logging is disposed before flushing.
    /// </summary>
    /// <returns>The emergency logger factory.</returns>
    protected ILoggerFactory GetEmergencyLoggerFactory()
    {
        try
        {
            return emergencyLoggerFactory ??= MakeEmergencyLoggerFactory();
        }
        catch (Exception)
        {
            return NullLoggerFactory.Instance;
        }
    }

    /// <summary>
    /// Creates the emergency logger factory used when deferred logging is disposed before flushing.
    /// </summary>
    /// <returns>The emergency logger factory.</returns>
    protected virtual ILoggerFactory MakeEmergencyLoggerFactory()
    {
        return services?.BuildServiceProvider().GetRequiredService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
    }

    /// <summary>
    /// Gets the emergency activity lifecycle log emitter used when deferred logging is disposed before flushing.
    /// </summary>
    /// <returns>The emergency activity lifecycle log emitter.</returns>
    protected ActivityLifecycleLogEmitter GetEmergencyLogEmitter()
    {
        try
        {
            return emergencyLogEmitter ??= MakeEmergencyLogEmitter();
        }
        catch (Exception)
        {
            return ActivityLifecycleLogEmitter.Noop;
        }
    }

    /// <summary>
    /// Creates the emergency activity lifecycle log emitter used when deferred logging is disposed before flushing.
    /// </summary>
    /// <returns>The emergency activity lifecycle log emitter.</returns>
    protected virtual ActivityLifecycleLogEmitter MakeEmergencyLogEmitter()
    {
        return services?.BuildServiceProvider().GetRequiredService<ActivityLifecycleLogEmitter>() ?? ActivityLifecycleLogEmitter.Noop;
    }

    /// <summary>
    /// Attaches this manager to a service collection so deferred operations are flushed when the service provider is created.
    /// </summary>
    /// <param name="services">The service collection to attach to.</param>
    public void AttachTo([SuppressMessage("ReSharper", "ParameterHidesMember")] IServiceCollection services)
    {
        services.FlushOnCreateServiceProvider(loggerFactory);
        services.FlushOnCreateServiceProvider(logEmitter);

        this.services = services;

        AdditionalAttachTo(services);
    }

    /// <summary>
    /// Performs additional work when this manager is attached to a service collection.
    /// </summary>
    /// <param name="services">The service collection to attach to.</param>
    protected virtual void AdditionalAttachTo([SuppressMessage("ReSharper", "ParameterHidesMember")] IServiceCollection services) { }

    /// <inheritdoc />
    public void Dispose()
    {
        AdditionalDispose();

        logEmitter.Dispose();
        loggerFactory.Dispose();
        operationRegistry.Dispose();
    }

    /// <summary>
    /// Performs additional work when this manager is disposed.
    /// </summary>
    protected virtual void AdditionalDispose() { }
}

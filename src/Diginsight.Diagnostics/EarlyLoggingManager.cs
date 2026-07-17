using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Diginsight.Diagnostics;

public class EarlyLoggingManager : IDisposable
{
    private readonly DeferredOperationRegistry operationRegistry;
    private readonly DeferredLoggerFactory loggerFactory;
    private readonly DeferredActivityLifecycleLogEmitter logEmitter;

    protected IServiceCollection? services;

    private ILoggerFactory? emergencyLoggerFactory;
    private ActivityLifecycleLogEmitter? emergencyLogEmitter;

    public ILoggerFactory LoggerFactory => loggerFactory;

    public EarlyLoggingManager(Func<ActivitySource, bool> shouldListenTo, TimeProvider? timeProvider = null)
    {
        operationRegistry = new DeferredOperationRegistry();

        loggerFactory = new DeferredLoggerFactory(operationRegistry, timeProvider, GetEmergencyLoggerFactory);
        logEmitter = new DeferredActivityLifecycleLogEmitter(operationRegistry, shouldListenTo, timeProvider, GetEmergencyLogEmitter);
    }

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

    protected virtual ILoggerFactory MakeEmergencyLoggerFactory()
    {
        return services?.BuildServiceProvider().GetRequiredService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
    }

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

    protected virtual ActivityLifecycleLogEmitter MakeEmergencyLogEmitter()
    {
        return services?.BuildServiceProvider().GetRequiredService<ActivityLifecycleLogEmitter>() ?? ActivityLifecycleLogEmitter.Noop;
    }

    public void AttachTo([SuppressMessage("ReSharper", "ParameterHidesMember")] IServiceCollection services)
    {
        services.FlushOnCreateServiceProvider(loggerFactory);
        services.FlushOnCreateServiceProvider(logEmitter);

        this.services = services;

        AdditionalAttachTo(services);
    }

    protected virtual void AdditionalAttachTo([SuppressMessage("ReSharper", "ParameterHidesMember")] IServiceCollection services) { }

    public void Dispose()
    {
        AdditionalDispose();

        logEmitter.Dispose();
        loggerFactory.Dispose();
        operationRegistry.Dispose();
    }

    protected virtual void AdditionalDispose() { }
}

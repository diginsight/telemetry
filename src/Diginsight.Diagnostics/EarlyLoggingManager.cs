using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

public class EarlyLoggingManager : IDisposable
{
    private readonly DeferredOperationRegistry operationRegistry;
    private readonly DeferredLoggerFactory loggerFactory;
    private readonly DeferredActivityLifecycleLogEmitter logEmitter;

    private ILoggerFactory? emergencyLoggerFactory;
    private ActivityLifecycleLogEmitter? emergencyLogEmitter;

    public ILoggerFactory LoggerFactory => loggerFactory;

    public EarlyLoggingManager(Func<ActivitySource, bool> shouldListenTo, TimeProvider? timeProvider = null)
    {
        operationRegistry = new DeferredOperationRegistry();

        loggerFactory = new DeferredLoggerFactory(operationRegistry, timeProvider, GetEmergencyLoggerFactory);
        logEmitter = new DeferredActivityLifecycleLogEmitter(operationRegistry, shouldListenTo, timeProvider, GetEmergencyLogEmitter);
    }

    private ILoggerFactory GetEmergencyLoggerFactory() => emergencyLoggerFactory ??= MakeEmergencyLoggerFactory();

    protected virtual ILoggerFactory MakeEmergencyLoggerFactory() => NullLoggerFactory.Instance;

    private ActivityLifecycleLogEmitter GetEmergencyLogEmitter() => emergencyLogEmitter ??= MakeEmergencyLogEmitter();

    protected virtual ActivityLifecycleLogEmitter MakeEmergencyLogEmitter() => ActivityLifecycleLogEmitter.Noop;

    public virtual void AttachTo(IServiceCollection services)
    {
        services.FlushOnCreateServiceProvider(loggerFactory);
        services.FlushOnCreateServiceProvider(logEmitter);

        AdditionalAttachTo(services);
    }

    protected virtual void AdditionalAttachTo(IServiceCollection services) { }

    public void Dispose()
    {
        AdditionalDispose();

        logEmitter.Dispose();
        loggerFactory.Dispose();
        operationRegistry.Dispose();
    }

    protected virtual void AdditionalDispose() { }
}

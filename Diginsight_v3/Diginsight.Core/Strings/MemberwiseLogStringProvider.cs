using Microsoft.Extensions.Options;
using System.Collections;

namespace Diginsight.Strings;

internal sealed class MemberwiseLogStringProvider : ReflectionLogStringProvider
{
    private readonly ILogStringConfiguration logStringConfiguration;
    private readonly IReflectionLogStringHelper helper;
    private readonly ILogStringTypeContractAccessor contractAccessor;

    private readonly IDictionary<Type, Handling> handlingCache = new Dictionary<Type, Handling>();

    public MemberwiseLogStringProvider(
        IOptions<LogStringConfiguration> logStringConfigurationOptions,
        IReflectionLogStringHelper helper,
        IOptions<LogStringTypeContractAccessor> contractAccessorOptions
    )
    {
        this.helper = helper;
        logStringConfiguration = logStringConfigurationOptions.Value;
        contractAccessor = contractAccessorOptions.Value;
    }

    protected override Handling IsHandled(Type type)
    {
        lock (((ICollection)handlingCache).SyncRoot)
        {
            return handlingCache.TryGetValue(type, out Handling handling)
                ? handling
                : handlingCache[type] = IsHandledCore();

            Handling IsHandledCore()
            {
                foreach (Type t in type.GetClosure())
                {
                    if (contractAccessor.TryGet(t) is { } typeContract)
                    {
                        switch (typeContract.Included)
                        {
                            case true:
                                return Handling.Handle;

                            case false:
                                return Handling.Forbid;
                        }
                    }

                    if (t.IsDefined(typeof(LogStringableObjectAttribute), false))
                    {
                        return Handling.Handle;
                    }
                    if (t.IsDefined(typeof(NonLogStringableObjectAttribute), false))
                    {
                        return Handling.Forbid;
                    }
                }

                return logStringConfiguration.IsMemberwiseLogStringableByDefault ? Handling.Handle : Handling.Pass;
            }
        }
    }

    protected override ReflectionLogStringable MakeLogStringable(object obj) => new MemberwiseLogStringable(obj, helper, contractAccessor);
}

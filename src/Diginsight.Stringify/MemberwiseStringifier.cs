using Microsoft.Extensions.Options;
using System.Collections;

namespace Diginsight.Stringify;

internal sealed class MemberwiseStringifier : ReflectionStringifier
{
    private readonly IStringifyOverallConfiguration overallConfiguration;
    private readonly IReflectionStringifyHelper helper;
    private readonly IStringifyTypeContractAccessor contractAccessor;

    private readonly IDictionary<Type, Handling> handlingCache = new Dictionary<Type, Handling>();

    public MemberwiseStringifier(
        IOptions<StringifyOverallConfiguration> overallConfigurationOptions,
        IReflectionStringifyHelper helper,
        IOptions<StringifyTypeContractAccessor> contractAccessorOptions
    )
    {
        this.helper = helper;
        overallConfiguration = overallConfigurationOptions.Value;
        contractAccessor = contractAccessorOptions.Value;
    }

    protected override Handling IsHandled(Type type)
    {
        if (handlingCache.TryGetValue(type, out Handling handling))
        {
            return handling;
        }

        lock (((ICollection)handlingCache).SyncRoot)
        {
            return handlingCache.TryGetValue(type, out handling)
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

                    if (t.IsDefined(typeof(StringifiableTypeAttribute), false))
                    {
                        return Handling.Handle;
                    }
                    if (t.IsDefined(typeof(NonStringifiableObjectAttribute), false))
                    {
                        return Handling.Forbid;
                    }
                }

                return overallConfiguration.IsMemberwiseStringifiableByDefault ? Handling.Handle : Handling.Pass;
            }
        }
    }

    protected override ReflectionStringifiable MakeStringifiable(object obj) => new MemberwiseStringifiable(obj, helper, contractAccessor);
}

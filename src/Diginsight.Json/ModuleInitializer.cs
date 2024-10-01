using Diginsight.Runtime;
using Diginsight.Stringify;
#if NET
using System.ComponentModel;
using System.Runtime.CompilerServices;
#endif

namespace Diginsight.Json;

#if NET
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class ModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
#else
public static class ModuleInitializer
{
    private static volatile bool initialized = false;

    public static void Initialize()
    {
        if (initialized)
            return;
        initialized = true;
#endif

        RuntimeUtils.HeuristicSizeProviders.Add(JTokenHeuristicSizeProvider.Instance);

        StringifyOverallConfiguration.GlobalCustomRegistrations.Add(
            new StringifierRegistration(typeof(JTokenStringifier), int.MaxValue)
        );
    }
}

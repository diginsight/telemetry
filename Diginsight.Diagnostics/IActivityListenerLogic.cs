using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IActivityListenerLogic
{
    void ActivityStarted(Activity activity)
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    { }
#else
    ;
#endif

    void ActivityStopped(Activity activity)
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    { }
#else
    ;
#endif

    ActivitySamplingResult Sample(ref ActivityCreationOptions<ActivityContext> creationOptions)
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        => ActivitySamplingResult.PropagationData;
#else
    ;
#endif
}

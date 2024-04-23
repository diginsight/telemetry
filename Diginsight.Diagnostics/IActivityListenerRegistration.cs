using System.Diagnostics;

namespace Diginsight.Diagnostics;

public interface IActivityListenerRegistration
{
    IActivityListenerLogic Logic { get; }

    bool ShouldListenTo(ActivitySource activitySource);
}

using OpenTelemetry;
using System.Diagnostics;

namespace Diginsight.Diagnostics;

internal sealed class DiginsightLogProcessor : BaseProcessor<Activity>
{
    private readonly ActivityLifecycleLogEmitter emitter;

    public DiginsightLogProcessor(ActivityLifecycleLogEmitter emitter)
    {
        this.emitter = emitter;
    }

    public override void OnStart(Activity activity) => emitter.OnStart(activity);

    public override void OnEnd(Activity activity) => emitter.OnEnd(activity);
}

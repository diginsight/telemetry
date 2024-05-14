#if EXPERIMENT_DECOMPOSING
namespace Diginsight.Decomposing;

public abstract class JComposerBase : IJComposer
{
    public bool IsUsed { get; private set; }

    protected void SetUsed()
    {
        if (IsUsed)
        {
            throw new InvalidOperationException("Composer already used");
        }
        IsUsed = true;
    }
}
#endif

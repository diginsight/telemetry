#if EXPERIMENT_DECOMPOSING
namespace Diginsight.Decomposing;

public interface IJArrayComposer : IJContainerComposer
{
    IJArrayComposer Item(Action<IJTokenComposer> makeValue);
}
#endif

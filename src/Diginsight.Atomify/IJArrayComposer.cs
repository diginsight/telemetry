#if EXPERIMENT_ATOMIFY
namespace Diginsight.Atomify;

public interface IJArrayComposer : IJContainerComposer
{
    IJArrayComposer Item(Action<IJTokenComposer> makeValue);
}
#endif

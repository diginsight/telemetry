#if EXPERIMENT_ATOMIFY
namespace Diginsight.Atomify;

public interface IJObjectComposer : IJContainerComposer
{
    IJObjectComposer Property(string name, Action<IJTokenComposer> makeValue);
}
#endif

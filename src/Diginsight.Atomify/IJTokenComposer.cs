#if EXPERIMENT_ATOMIFY
namespace Diginsight.Atomify;

public interface IJTokenComposer : IJComposer
{
    IJObjectComposer Object();

    IJArrayComposer Array();

    void Value(object value);
}
#endif

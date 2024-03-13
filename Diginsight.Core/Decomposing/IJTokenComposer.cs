#if EXPERIMENT_DECOMPOSING
namespace Diginsight.Decomposing;

public interface IJTokenComposer : IJComposer
{
    IJObjectComposer Object();

    IJArrayComposer Array();

    void Value(object value);
}
#endif

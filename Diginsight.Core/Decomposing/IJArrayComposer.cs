namespace Diginsight.Decomposing;

public interface IJArrayComposer : IJContainerComposer
{
    IJArrayComposer Item(Action<IJTokenComposer> makeValue);
}

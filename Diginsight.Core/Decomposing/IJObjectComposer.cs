namespace Diginsight.Decomposing;

public interface IJObjectComposer : IJContainerComposer
{
    IJObjectComposer Property(string name, Action<IJTokenComposer> makeValue);
}

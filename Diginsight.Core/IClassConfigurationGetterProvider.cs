namespace Diginsight;

public interface IClassConfigurationGetterProvider
{
    IClassConfigurationGetter GetFor(Type @class);
}

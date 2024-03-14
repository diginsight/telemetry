#if EXPERIMENT_CLASS_CONFIGURATION_GETTER
namespace Diginsight;

public interface IClassConfigurationGetterProvider
{
    IClassConfigurationGetter GetFor(Type @class);
}
#endif

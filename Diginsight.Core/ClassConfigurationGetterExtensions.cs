#if EXPERIMENT_CLASS_CONFIGURATION_GETTER
namespace Diginsight;

public static class ClassConfigurationGetterExtensions
{
    public static IClassConfigurationGetter<TClass> GetFor<TClass>(this IClassConfigurationGetterProvider classConfigurationGetterProvider)
    {
        return (IClassConfigurationGetter<TClass>)classConfigurationGetterProvider.GetFor(typeof(TClass));
    }

    public static T Get<T>(
        this IClassConfigurationGetter classConfigurationGetter,
        string key,
        T defaultValue = default!, IClassConfigurationGetter.SafeConverter<T>? tryConvert = null
    )
    {
        return classConfigurationGetter.TryGet(key, out T value, tryConvert) ? value : defaultValue;
    }
}
#endif

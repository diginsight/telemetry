namespace Diginsight;

public static class ClassConfigurationGetterExtensions
{
    public static IClassConfigurationGetter<TClass> GetFor<TClass>(this IClassConfigurationGetterProvider classConfigurationGetterProvider)
    {
        return (IClassConfigurationGetter<TClass>)classConfigurationGetterProvider.GetFor(typeof(TClass));
    }
}

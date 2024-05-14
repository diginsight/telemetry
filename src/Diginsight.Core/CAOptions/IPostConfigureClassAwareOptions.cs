namespace Diginsight.CAOptions;

public interface IPostConfigureClassAwareOptions<in TOptions>
    where TOptions : class
{
    void PostConfigure(string name, Type @class, TOptions options);
}

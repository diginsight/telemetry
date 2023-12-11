namespace Diginsight.CAOptions;

public interface IConfigureClassAwareOptions<in TOptions>
    where TOptions : class
{
    void Configure(string name, Type @class, TOptions options);
}

namespace Diginsight.CAOptions;

public interface IClassAwareOptionsFactory<out TOptions>
    where TOptions : class
{
    TOptions Create(string name, Type? type);
}
